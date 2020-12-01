using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    internal sealed class Parser
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _currentTokenId;

        public DiagnosticBag Diagnostics { get; }

        public Parser(SyntaxTree syntaxTree)
        {
            Diagnostics = new DiagnosticBag();
            var lexer = new Lexer(syntaxTree);

            SyntaxToken token;
            var tokens = new List<SyntaxToken>();
            var badTokens = new List<SyntaxToken>();

            do
            {
                token = lexer.Lex();

                if (token.Kind == SyntaxKind.BadToken)
                {
                    badTokens.Add(token);
                    continue;
                }

                if (badTokens.Count > 0)
                {
                    token = HandleBadTokens(syntaxTree, token, badTokens);
                }
                tokens.Add(token);
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
            _tokens = tokens.ToImmutableArray();
            Diagnostics.AddRange(lexer.Diagnostics);
        }

        private static SyntaxToken HandleBadTokens(SyntaxTree syntaxTree, SyntaxToken token, List<SyntaxToken> badTokens)
        {
            var leadingTriviaBuilder = token.LeadingTrivia.ToBuilder();
            var index = 0;
            foreach (var badToken in badTokens)
            {
                foreach (var lt in badToken.LeadingTrivia)
                {
                    leadingTriviaBuilder.Insert(index++, lt);
                }
                var trivia = new SyntaxTrivia(syntaxTree,
                                              SyntaxKind.SkippedTextTrivia,
                                              badToken.Position,
                                              badToken.Text);
                leadingTriviaBuilder.Insert(index++, trivia);


                foreach (var tt in badToken.TrailingTrivia)
                {
                    leadingTriviaBuilder.Insert(index++, tt);
                }
            }
            badTokens.Clear();
            token = new SyntaxToken(token.SyntaxTree,
                                    token.Kind,
                                    token.Position,
                                    token.Text,
                                    token.Value,
                                    leadingTriviaBuilder.ToImmutable(),
                                    token.TrailingTrivia);
            return token;
        }

        private SyntaxToken Peek(int offset)
        {
            var index = _currentTokenId + offset;
            if (index >= _tokens.Length)
            {
                return _tokens[^1];
            }
            return _tokens[index];
        }

        private SyntaxToken Current => Peek(0);

        private SyntaxToken NextToken()
        {
            var current = Current;
            _currentTokenId++;
            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
            {
                return NextToken();
            }

            Diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kind);
            return new SyntaxToken(_syntaxTree,
                                   kind,
                                   Current.Position,
                                   null,
                                   null,
                                   ImmutableArray<SyntaxTrivia>.Empty,
                                   ImmutableArray<SyntaxTrivia>.Empty);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var members = ParseMembers();
            var endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(_syntaxTree, members, endOfFileToken);
        }

        private ImmutableArray<MemberSyntax> ParseMembers()
        {
            var members = ImmutableArray.CreateBuilder<MemberSyntax>();

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var startToken = Current;

                var member = ParseMember();
                members.Add(member);

                // If ParseMember() did not consume any tokens,
                // we need to skip the current token and continue
                // in order to avoid an infinite loop.
                //
                // We don't need to report an error, because we'll
                // already tried to parse an expression statement
                // and reported one.
                if (Current == startToken)
                {
                    NextToken();
                }
            }

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.FunctionKeyword:
                    return ParseFunctionDeclaration();
                case SyntaxKind.EnumKeyword:
                    return ParseEnumDeclaration();
                case SyntaxKind.StructKeyword:
                    return ParseStructDeclaration();
                default:
                    return ParseGlobalStatement();
            }
        }

        private MemberSyntax ParseFunctionDeclaration()
        {
            SyntaxToken identifier;
            SyntaxToken? dotToken, receiver;

            var functionKeyword = MatchToken(SyntaxKind.FunctionKeyword);

            if (Current.Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.DotToken)
            {
                receiver = MatchToken(SyntaxKind.IdentifierToken);
                dotToken = MatchToken(SyntaxKind.DotToken);
                identifier = MatchToken(SyntaxKind.IdentifierToken);
            }
            else
            {
                receiver = null;
                dotToken = null;
                identifier = MatchToken(SyntaxKind.IdentifierToken);
            }

            var openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var parameters = ParseParameterList();
            var closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            var type = ParseOptionalTypeClause();
            var body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(_syntaxTree, functionKeyword, receiver, dotToken, identifier, openParenthesisToken, parameters, closeParenthesisToken, type, body);
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameterList()
        {
            return ParseList(SyntaxKind.CloseParenthesisToken, SyntaxKind.CommaToken, ParseParameter);
        }

        private ParameterSyntax ParseParameter()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var type = ParseTypeClause();
            return new ParameterSyntax(_syntaxTree, identifier, type);
        }

        private MemberSyntax ParseEnumDeclaration()
        {
            var enumKeyword = MatchToken(SyntaxKind.EnumKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);
            var values = ParseEnumValueList();
            var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);
            return new EnumDeclarationSyntax(_syntaxTree, enumKeyword, identifier, openBraceToken, values, closeBraceToken);
        }

        private SeparatedSyntaxList<EnumSyntax> ParseEnumValueList()
        {
            return ParseList(SyntaxKind.CloseBraceToken, SyntaxKind.CommaToken, ParseEnum);
        }

        private EnumSyntax ParseEnum()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var valueClause = ParseOptionalEnumValueClause();
            return new EnumSyntax(_syntaxTree, identifier, valueClause);
        }

        private EnumValueClauseSyntax? ParseOptionalEnumValueClause()
        {
            if (Current.Kind == SyntaxKind.EqualsToken)
            {
                var equalsToken = MatchToken(SyntaxKind.EqualsToken);
                var expression = ParseBinaryExpression();
                return new EnumValueClauseSyntax(_syntaxTree, equalsToken, expression);
            }
            return null;
        }

        private MemberSyntax ParseStructDeclaration()
        {
            var keyword = MatchToken(SyntaxKind.StructKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var body = ParseStructBlockStatement();

            return new StructDeclarationSyntax(_syntaxTree, keyword, identifier, body);
        }

        private MemberBlockStatementSyntax ParseStructBlockStatement()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                   Current.Kind != SyntaxKind.CloseBraceToken)
            {
                var startToken = Current;

                var statement = ParseVariableDeclarationStatement();
                statements.Add(statement);

                // If ParseStatement() did not consume any tokens,
                // we need to skip the current token and continue
                // in order to avoid an infinite loop.
                //
                // We don't need to report an error, because we'll
                // already tried to parse an expression statement
                // and reported one.
                if (Current == startToken)
                {
                    NextToken();
                }
            }

            var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);
            return new MemberBlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private SeparatedSyntaxList<T> ParseList<T>(SyntaxKind endTokenKind, SyntaxKind separatorKind, Func<T> parseMethod) where T : SyntaxNode
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();
            var parseNextArgument = true;
            while (parseNextArgument &&
                   Current.Kind != endTokenKind &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var parameter = parseMethod.Invoke();
                nodesAndSeparators.Add(parameter);

                if (Current.Kind == separatorKind)
                {
                    var comma = MatchToken(separatorKind);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    parseNextArgument = false;
                }
            }

            return new SeparatedSyntaxList<T>(nodesAndSeparators.ToImmutable());
        }

        private MemberSyntax ParseGlobalStatement()
        {
            var statement = ParseStatement();
            return new GlobalStatementSyntax(_syntaxTree, statement);
        }

        public StatementSyntax ParseStatement()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();
                case SyntaxKind.ConstKeyword:
                case SyntaxKind.VarKeyword:
                    return ParseVariableDeclarationStatement();
                case SyntaxKind.IfKeyword:
                    return ParseIfStatement();
                case SyntaxKind.DoKeyword:
                    return ParseDoWhileStatement();
                case SyntaxKind.WhileKeyword:
                    return ParseWhileStatement();
                case SyntaxKind.ForKeyword:
                    return ParseForStatement();
                case SyntaxKind.BreakKeyword:
                    return ParseBreakStatement();
                case SyntaxKind.ContinueKeyword:
                    return ParseContinueStatement();
                case SyntaxKind.ReturnKeyword:
                    return ParseReturnStatement();
                default:
                    return ParseExpressionStatement();
            }
        }

        private StatementSyntax ParseVariableDeclarationStatement()
        {
            var expectedToken = Current.Kind == SyntaxKind.VarKeyword ?
                                            SyntaxKind.VarKeyword :
                                            SyntaxKind.ConstKeyword;
            var keyword = MatchToken(expectedToken);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var typeClause = ParseOptionalTypeClause();

            // A type can be omitted when it can be inferred from the initializer
            // An initializer can be omitted when a type is present AND the variable is not read-only

            // A variable that is read-only must be initialized
            if (typeClause == null ||
                Current.Kind == SyntaxKind.EqualsToken ||
                expectedToken == SyntaxKind.ConstKeyword)
            {
                var equals = MatchToken(SyntaxKind.EqualsToken);
                var initializer = ParseExpression();

                return new VariableDeclarationStatementSyntax(_syntaxTree, keyword, identifier, typeClause, equals, initializer);
            }
            else
            {
                return new VariableDeclarationStatementSyntax(_syntaxTree, keyword, identifier, typeClause, null, null);
            }
        }

        private TypeClauseSyntax? ParseOptionalTypeClause()
        {
            if (Current.Kind != SyntaxKind.ColonToken)
            {
                return null;
            }

            return ParseTypeClause();
        }

        private TypeClauseSyntax ParseTypeClause()
        {
            var colonToken = MatchToken(SyntaxKind.ColonToken);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            return new TypeClauseSyntax(_syntaxTree, colonToken, identifier);
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
            var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken && Current.Kind != SyntaxKind.CloseBraceToken)
            {
                var startToken = Current;

                var statement = ParseStatement();
                statements.Add(statement);

                // If ParseStatement() did not consume any tokens,
                // we need to skip the current token and continue
                // in order to avoid an infinite loop.
                //
                // We don't need to report an error, because we'll
                // already tried to parse an expression statement
                // and reported one.
                if (Current == startToken)
                {
                    NextToken();
                }
            }

            var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);
            return new BlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private StatementSyntax ParseIfStatement()
        {
            var keyword = MatchToken(SyntaxKind.IfKeyword);
            var condition = ParseExpression();
            var thenStatement = ParseStatement();
            var elseClause = ParseOptionalElseClause();
            return new IfStatementSyntax(_syntaxTree, keyword, condition, thenStatement, elseClause);
        }

        private ElseClauseSyntax? ParseOptionalElseClause()
        {
            if (Current.Kind != SyntaxKind.ElseKeyword)
            {
                return null;
            }

            var keyword = NextToken();
            var elseStatement = ParseStatement();
            return new ElseClauseSyntax(_syntaxTree, keyword, elseStatement);
        }

        private StatementSyntax ParseDoWhileStatement()
        {
            var doKeyword = MatchToken(SyntaxKind.DoKeyword);
            var body = ParseStatement();
            var whileKeyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseExpression();
            return new DoWhileStatementSyntax(_syntaxTree, doKeyword, body, whileKeyword, condition);
        }

        private StatementSyntax ParseWhileStatement()
        {
            var keyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseExpression();
            var body = ParseStatement();
            return new WhileStatementSyntax(_syntaxTree, keyword, condition, body);
        }

        private StatementSyntax ParseForStatement()
        {
            var keyword = MatchToken(SyntaxKind.ForKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var equalsToken = MatchToken(SyntaxKind.EqualsToken);
            var lowerBound = ParseExpression();
            var toKeyword = MatchToken(SyntaxKind.ToKeyword);
            var upperBound = ParseExpression();
            var stepClause = ParseOptionalStepClause();
            var body = ParseStatement();
            return new ForStatementSyntax(_syntaxTree, keyword, identifier, equalsToken, lowerBound, toKeyword, upperBound, stepClause, body);
        }

        private StepClauseSyntax? ParseOptionalStepClause()
        {
            if (Current.Kind != SyntaxKind.StepKeyword)
            {
                return null;
            }

            var keyword = NextToken();
            var expression = ParseExpression();
            return new StepClauseSyntax(_syntaxTree, keyword, expression);
        }

        private StatementSyntax ParseBreakStatement()
        {
            var keyword = MatchToken(SyntaxKind.BreakKeyword);
            return new BreakStatementSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseContinueStatement()
        {
            var keyword = MatchToken(SyntaxKind.ContinueKeyword);
            return new ContinueStatementSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseReturnStatement()
        {
            var keyword = MatchToken(SyntaxKind.ReturnKeyword);
            var keywordLine = _text.GetLineIndex(keyword.Span.Start);
            var currentLine = _text.GetLineIndex(Current.Span.Start);
            var isEof = Current.Kind == SyntaxKind.EndOfFileToken;
            var sameLine = !isEof && keywordLine == currentLine;
            var expression = sameLine ? ParseExpression() : null;
            return new ReturnStatementSyntax(_syntaxTree, keyword, expression);
        }

        private StatementSyntax ParseExpressionStatement()
        {
            var expression = ParseExpression();
            return new ExpressionStatementSyntax(_syntaxTree, expression);
        }

        private ExpressionSyntax ParseExpression()
        {
            return ParseBinaryExpression();
        }

        /// <summary>
        /// UnaryExpr := (Op)? Expr
        /// BinaryExpr := UnaryExpr Op BinaryExpr
        /// </summary>
        /// <param name="parentPrecedence"></param>
        /// <returns></returns>
        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            var unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();

            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                var operatorToken = NextToken();
                var operand = ParseBinaryExpression(unaryOperatorPrecedence);
                left = new UnaryExpressionSyntax(_syntaxTree, operatorToken, operand);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                var precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                {
                    break;
                }

                var operatorToken = NextToken();
                var right = ParseBinaryExpression(precedence);
                left = new BinaryExpressionSyntax(_syntaxTree, left, operatorToken, right);
            }
            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                    return ParseParenthesizedExpression();

                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                    return ParseBooleanLiteral();

                case SyntaxKind.NumberToken:
                    return ParseNumberLiteral();

                case SyntaxKind.StringToken:
                    return ParseStringLiteral();

                case SyntaxKind.DefaultKeyword:
                    return ParseDefaultLiteral();

                default:
                    return ParseNameOrCallExpression(true);
            }
        }

        private ExpressionSyntax ParseNumberLiteral()
        {
            var numberToken = MatchToken(SyntaxKind.NumberToken);
            return new LiteralExpressionSyntax(_syntaxTree, numberToken);
        }

        private ExpressionSyntax ParseStringLiteral()
        {
            var stringToken = MatchToken(SyntaxKind.StringToken);
            return new LiteralExpressionSyntax(_syntaxTree, stringToken);
        }

        private ExpressionSyntax ParseDefaultLiteral()
        {
            var defaultKeywordToken = MatchToken(SyntaxKind.DefaultKeyword);
            return new DefaultKeywordSyntax(_syntaxTree, defaultKeywordToken);
        }

        private ExpressionSyntax ParseParenthesizedExpression()
        {
            var left = MatchToken(SyntaxKind.OpenParenthesisToken);
            var expression = ParseExpression();
            var right = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(_syntaxTree, left, expression, right);
        }

        private ExpressionSyntax ParseBooleanLiteral()
        {
            var isTrue = Current.Kind == SyntaxKind.TrueKeyword;
            var keywordToken = isTrue ? MatchToken(SyntaxKind.TrueKeyword) :
                                        MatchToken(SyntaxKind.FalseKeyword);
            return new LiteralExpressionSyntax(_syntaxTree, keywordToken, isTrue);
        }

        private NameExpressionSyntax ParseNameOrCallExpression(bool withSuffix = false)
        {
            if (Peek(1).Kind == SyntaxKind.OpenParenthesisToken)
            {
                return ParseCallExpression();
            }
            return ParseNameExpression(withSuffix);
        }

        private NameExpressionSyntax ParseNameExpression(bool withSuffix = false)
        {
            if (!withSuffix || Peek(1).Kind != SyntaxKind.DotToken)
            {
                var identifier = MatchToken(SyntaxKind.IdentifierToken);
                return new NameExpressionSyntax(_syntaxTree, identifier);
            }
            return ParseMemberAccess();
        }

        private CallExpressionSyntax ParseCallExpression()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var arguments = ParseArguments();
            var closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new CallExpressionSyntax(_syntaxTree, identifier, openParenthesisToken, arguments, closeParenthesisToken);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseArguments()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            var parseNextParameter = true;
            while (parseNextParameter &&
                   Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var expression = ParseExpression();
                nodesAndSeparators.Add(expression);

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    parseNextParameter = false;
                }
            }

            return new SeparatedSyntaxList<ExpressionSyntax>(nodesAndSeparators.ToImmutable());
        }
        
        private MemberAccessExpressionSyntax ParseMemberAccess()
        {
            var queue = new Queue<NameExpressionSyntax>();
            var dotTokenQueue = new Queue<SyntaxToken>();
            var condition = true;

            while (condition)
            {
                queue.Enqueue(ParseNameOrCallExpression());

                if (Current.Kind == SyntaxKind.DotToken)
                {
                    dotTokenQueue.Enqueue(MatchToken(SyntaxKind.DotToken));
                }
                else
                {
                    condition = false;
                }
            }

            var first = queue.Dequeue();
            return ParseMemberAccessInternal(queue, dotTokenQueue, first);
        }

        /// <summary>
        /// MemberAccessExpr := IDENT (DOT IDENT)*
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="dotTokenQueue"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private MemberAccessExpressionSyntax ParseMemberAccessInternal(Queue<NameExpressionSyntax> queue, Queue<SyntaxToken> dotTokenQueue, NameExpressionSyntax parent)
        {
            var member = queue.Dequeue();
            var operatorToken = dotTokenQueue.Dequeue();
            parent = new MemberAccessExpressionSyntax(_syntaxTree, parent, operatorToken, member);
            while (queue.Count > 0)
            {
                member = queue.Dequeue();
                operatorToken = dotTokenQueue.Dequeue();
                parent = new MemberAccessExpressionSyntax(_syntaxTree, parent, operatorToken, member);
            }
            return (MemberAccessExpressionSyntax)parent;
        }
    }
}