﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Compiler.CodeAnalysis.Binding.FlowControl;
using Compiler.CodeAnalysis.Binding.Scopes;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Lowering;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        private int _labelCounter;
        private IBoundScope _scope;
        private readonly FunctionSymbol? _function;
        private readonly TypeSymbol? _type;
        private readonly Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _loopStack;

        public DiagnosticBag Diagnostics { get; }

        private Binder(IBoundScope scope, FunctionSymbol? function, TypeSymbol? type)
        {
            _scope = scope;
            _function = function;
            _type = type;
            Diagnostics = scope.Diagnostics;
            _loopStack = new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();
        }

        public static BoundGlobalScope BindGlobalScope(ImmutableArray<SyntaxTree> syntaxTrees)
        {
            var parentScope = CreateRootScope();
            var binder = new Binder(parentScope, null, null);
            binder.Diagnostics.AddRange(syntaxTrees.SelectMany(st => st.Diagnostics));
            
            var typeDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                              .OfType<TypeDeclarationSyntax>();

            foreach (var typeDeclaration in typeDeclarations)
            {
                binder.BindTypeDeclaration(typeDeclaration);
            }

            var functionDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<FunctionDeclarationSyntax>();
            foreach (var functionSyntax in functionDeclarations)
            {
                var function = binder.BindFunctionDeclaration(functionSyntax);
                binder.Diagnostics.AddRange(function.BoundScope!.Diagnostics);
            }

            // Check for main/script with global statements
            var functions = binder._scope.GetDeclaredSymbols<FunctionSymbol>();

            FunctionSymbol? mainFunction;

            mainFunction = functions.FirstOrDefault(f => f.Name == "main");
            if (mainFunction != null && (mainFunction.ReturnType != TypeSymbol.Void || mainFunction.Parameters.Any()))
            {
                binder.Diagnostics.ReportMainMustHaveCorrectSignature(mainFunction.Declaration!.Identifier.Location);
            }

            var diagnostics = binder.Diagnostics.ToImmutableArray();
            var types = binder._scope.GetDeclaredSymbols<TypeSymbol>();
            return new BoundGlobalScope(diagnostics, mainFunction, functions, types);
        }

        private void BindTypeDeclaration(TypeDeclarationSyntax typeDeclarationSyntax)
        {
            TypeSymbol type;
            switch (typeDeclarationSyntax.TypeKind)
            {
                case TypeDeclarationKind.Enum:
                    type = BindEnumDeclaration((EnumDeclarationSyntax)typeDeclarationSyntax);
                    break;
                case TypeDeclarationKind.Struct:
                    type = BindStructDeclaration((StructDeclarationSyntax)typeDeclarationSyntax);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected declaration kind {typeDeclarationSyntax.Kind}");
            }

            _scope.TryDeclareType(type);

            _scope = type.BoundScope!;

            // Declare all nested types
            foreach (var statementSyntax in typeDeclarationSyntax.Body.Statement.OfType<TypeDeclarationSyntax>())
            {
                BindTypeDeclaration(statementSyntax);
            }

            _scope = type.BoundScope!.Parent!;
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(globalScope.Diagnostics);

            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();

            var functionsToLower = new List<FunctionSymbol>(globalScope.Functions.Where(f => f.Declaration != null));

            foreach (var typeSymbol in globalScope.Types)
            {
                Debug.Assert(typeSymbol.BoundScope != null);
                var binder = new Binder(typeSymbol.BoundScope, null, typeSymbol);
                binder.BindMemberBlockStatement(typeSymbol.Declaration!.Body, functionsToLower);
                diagnostics.AddRange(binder.Diagnostics);
            }

            foreach (var function in functionsToLower)
            {
                // Structs generate declartions for their constructors.  However, these have no function bodies.
                // We will skip attempting to lower the bodies for these and allow the Emitter to automatically
                // generate the code necessary.  This will avoid the potential of reporting diagnostic errors to
                // the user for code they never wrote.
                if (function.ReturnType is StructSymbol && function.Name.EndsWith(".ctor"))
                {
                    continue;
                }

                Debug.Assert(function.BoundScope != null);
                var binder = new Binder(function.BoundScope, function, function.ReceiverType);
                var body = binder.BindStatement(function.Declaration!.Body);
                var loweredBody = Lowerer.Lower(function, body);

                if (function.ReturnType != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                {
                    binder.Diagnostics.ReportAllPathsMustReturn(function.Declaration!.Identifier.Location);
                }

                functionBodies.Add(function, loweredBody);
                diagnostics.AddRange(binder.Diagnostics);
            }

            return new BoundProgram(diagnostics.ToImmutable(),
                                    globalScope.MainFunction,
                                    functionBodies.ToImmutable(),
                                    globalScope.Types);
        }

        private FunctionSymbol BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
        {
            var parameters = ImmutableArray.CreateBuilder<VariableSymbol>();
            var seenParameterNames = new HashSet<string>();

            foreach (var parameterSyntax in syntax.Parameters)
            {
                var parameterName = parameterSyntax.Identifier.Text;
                var parameterType = BindTypeClause(parameterSyntax.Type);
                if (!seenParameterNames.Add(parameterName))
                {
                    Diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                }
                else
                {
                    var parameter = VariableSymbol.Parameter(parameterSyntax, parameterName, false, parameterType);
                    parameters.Add(parameter);
                }
            }

            var returnType = BindTypeClause(syntax.Type) ?? TypeSymbol.Void;
            var receiver = _type;
            var function = new FunctionSymbol(syntax.Identifier.Text,
                                              parameters.ToImmutable(),
                                              returnType,
                                              syntax,
                                              _scope,
                                              receiver);

            _scope.TryDeclareFunction(function);
            return function;
        }

        private EnumSymbol BindEnumDeclaration(EnumDeclarationSyntax syntax)
        {
            string enumIdentifier = syntax.Identifier.Text;
            return new EnumSymbol(enumIdentifier, syntax, _scope);
        }

        private StructSymbol BindStructDeclaration(StructDeclarationSyntax syntax)
        {
            string structIdentifier = syntax.Identifier.Text;
            return new StructSymbol(structIdentifier, syntax, _scope);
        }

        private void BindMemberBlockStatement(MemberBlockStatementSyntax syntax, List<FunctionSymbol> functionsToLower)
        {
            var typeScope = _scope as TypeBoundScope;
            Debug.Assert(typeScope != null);
            
            var type = typeScope.OwnerType;
            foreach (var statementSyntax in syntax.Statement.OfType<TypeDeclarationSyntax>())
            {
                var nestedType = typeScope.TryLookupSymbol<TypeSymbol>(statementSyntax.Identifier.Text);
                Debug.Assert(nestedType != null);
                Debug.Assert(nestedType.BoundScope != null);

                var binder = new Binder(nestedType.BoundScope, null, nestedType);
                binder.BindMemberBlockStatement(nestedType.Declaration!.Body, functionsToLower);
                Diagnostics.AddRange(binder.Diagnostics);
            }

            foreach (var statementSyntax in syntax.Statement)
            {
                switch (statementSyntax.Kind)
                {
                    case SyntaxKind.VariableDeclarationStatement:
                        var variableStatement = BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)statementSyntax, false);
                        var field = new FieldSymbol((BoundVariableDeclarationStatement)variableStatement, type);
                        typeScope.TryDeclareField(field);
                        break;

                    case SyntaxKind.EnumElementDeclarationStatement:
                        int nextValue = 0;
                        var enumSyntax = (EnumValuesStatementSyntax)statementSyntax;
                        var usedValues = new Dictionary<int, string>();
                        foreach (var enumValueSyntax in enumSyntax.Values)
                        {
                            var enumElement = BindEnumElementStatement((EnumSymbol)type, enumValueSyntax, nextValue);
                            var value = (int)enumElement.Constant!.Value;
                            if (usedValues.TryGetValue(value, out var originalName))
                            {
                                Diagnostics.ReportEnumerationAlreadyContainsValue(enumValueSyntax.Location, type.Name, value, originalName);
                            }
                            else
                            {
                                usedValues.Add(value, enumElement.Name);
                            }
                            typeScope.TryDeclareField(enumElement);
                            nextValue = value + 1;
                        }
                        break;

                    case SyntaxKind.FunctionDeclaration:
                        var functionDeclarationSyntax = (FunctionDeclarationSyntax)statementSyntax;
                        var functionSymbol = BindFunctionDeclaration(functionDeclarationSyntax);
                        functionsToLower.Add(functionSymbol);
                        break;

                    case SyntaxKind.TypeDeclaration:
                        // Already declared
                        break;

                    default:
                        throw new InvalidOperationException($"Unexpected statement of kind {statementSyntax.Kind}");
                }
            }
        }

        private BoundStatement BindStatement(StatementSyntax syntax)
        {
            var result = BindStatementInternal(syntax);
            if (result is BoundExpressionStatement es)
            {
                var expression = es.Expression;
                if (expression is BoundMemberAccessExpression memberAccessExpression)
                {
                    expression = memberAccessExpression.Member;
                }
                var isAllowedExpression = expression.Kind == BoundNodeKind.ErrorExpression
                                          || expression.Kind == BoundNodeKind.CallExpression
                                          || expression.Kind == BoundNodeKind.AssignmentExpression
                                          || expression.Kind == BoundNodeKind.CompoundAssignmentExpression;

                if (!isAllowedExpression)
                {
                    Diagnostics.ReportInvalidExpressionStatement(syntax.Location);
                }
            }
            return result;
        }

        private BoundStatement BindStatementInternal(StatementSyntax statementSyntax)
        {
            switch (statementSyntax.Kind)
            {
                case SyntaxKind.BlockStatement:
                    return BindBlockStatement((BlockStatementSyntax)statementSyntax);
                case SyntaxKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax)statementSyntax);
                case SyntaxKind.VariableDeclarationStatement:
                    return BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)statementSyntax);
                case SyntaxKind.IfStatement:
                    return BindIfStatement((IfStatementSyntax)statementSyntax);
                case SyntaxKind.DoWhileStatement:
                    return BindDoWhileStatement((DoWhileStatementSyntax)statementSyntax);
                case SyntaxKind.WhileStatement:
                    return BindWhileStatement((WhileStatementSyntax)statementSyntax);
                case SyntaxKind.ForStatement:
                    return BindForStatement((ForStatementSyntax)statementSyntax);
                case SyntaxKind.ContinueStatement:
                    return BindContinueStatement((ContinueStatementSyntax)statementSyntax);
                case SyntaxKind.BreakStatement:
                    return BindBreakStatement((BreakStatementSyntax)statementSyntax);
                case SyntaxKind.ReturnStatement:
                    return BindReturnStatement((ReturnStatementSyntax)statementSyntax);
                default:
                    throw new InvalidOperationException($"Unexpected syntax {statementSyntax.Kind}");
            }
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            var statements = ImmutableArray.CreateBuilder<BoundStatement>();
            _scope = new BlockBoundScope(_scope);

            foreach (var statementSyntax in syntax.Statements)
            {
                var statement = BindStatement(statementSyntax);
                statements.Add(statement);
            }
            _scope = _scope.Parent!;
            return new BoundBlockStatement(syntax, statements.ToImmutable());
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var boundExpression = BindExpression(syntax.Expression, true);
            return new BoundExpressionStatement(syntax, boundExpression);
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax, bool addToScope = true)
        {
            var isReadOnly = syntax.Keyword.Kind == SyntaxKind.ConstKeyword;
            var type = BindTypeClause(syntax.TypeClause);

            if (syntax.Initializer != null && syntax.Initializer.Kind != SyntaxKind.DefaultKeyword)
            {
                var initializer = BindExpression(syntax.Initializer);
                var variableType = type ?? initializer.Type;
                var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, false, VariableKind.Local, variableType, initializer.ConstantValue, addToScope);
                var convertedInitializer = BindConversion(syntax.Initializer.Location, initializer, variableType);

                return new BoundVariableDeclarationStatement(syntax, variable, convertedInitializer);
            }
            else if (type != null)
            {
                var initializer = syntax.Initializer?.Kind == SyntaxKind.DefaultKeyword
                    ? BindDefaultExpression((DefaultKeywordSyntax)syntax.Initializer, type)
                    : BindSyntheticDefaultExpression(syntax, type);

                var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, false, VariableKind.Local, type, null, addToScope);
                var convertedInitializer = BindConversion(syntax.TypeClause!.Location, initializer!, type);

                return new BoundVariableDeclarationStatement(syntax, variable, convertedInitializer);
            }
            else
            {
                Diagnostics.ReportTypeNotFoundForDefault(syntax.Initializer!.Location);
                return BindErrorStatement(syntax);
            }
        }

        private FieldSymbol BindEnumElementStatement(EnumSymbol enumSymbol, EnumSyntax syntax, int nextValue)
        {
            var variableType = enumSymbol;
            if (syntax.ValueClause != null)
            {
                var boundValueExpression = BindExpression(syntax.ValueClause.Expression);
                nextValue = (int)boundValueExpression.ConstantValue!.Value;
            }
            var variable = BindVariableDeclaration(syntax.Identifier, true, true, VariableKind.Local, variableType, new BoundConstant(nextValue), false);
            var initializer = new BoundLiteralExpression(syntax, enumSymbol, nextValue);
            return new FieldSymbol(variable, enumSymbol, initializer);
        }

        private BoundExpression BindSyntheticDefaultExpression(VariableDeclarationStatementSyntax syntax, TypeSymbol type)
        {
            var syntaxToken = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.DefaultKeyword, syntax.Span.End, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
            var syntaxNode = new DefaultKeywordSyntax(syntax.SyntaxTree, syntaxToken);

            return BindDefaultExpression(syntaxNode, type);
        }

        private BoundExpression BindDefaultExpression(DefaultKeywordSyntax syntax, TypeSymbol type)
        {
            if (type is StructSymbol s)
            {
                // Struct types default to calling their empty constructor
                var ctorSyntaxExpression = new SyntaxToken(syntax.SyntaxTree,
                                                                SyntaxKind.IdentifierToken,
                                                                syntax.Span.End,
                                                                s.Name,
                                                                null,
                                                                ImmutableArray<SyntaxTrivia>.Empty,
                                                                ImmutableArray<SyntaxTrivia>.Empty);
                var openParenToken = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.OpenParenthesisToken, syntax.Span.End, "(", null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
                var closeParenToken = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.CloseParenthesisToken, syntax.Span.End, ")", null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);

                return BindCallExpression(new CallExpressionSyntax(syntax.SyntaxTree, ctorSyntaxExpression, openParenToken, new SeparatedSyntaxList<ExpressionSyntax>(ImmutableArray<SyntaxNode>.Empty), closeParenToken));
            }

            return new BoundLiteralExpression(syntax, type, type.DefaultValue!);
        }

        [return: NotNullIfNotNull("syntax")]
        private TypeSymbol? BindTypeClause(TypeClauseSyntax? syntax)
        {
            if (syntax == null)
            {
                return null;
            }

            var type = LookupType(syntax.Identifier.Text);
            if (type == null)
            {
                Diagnostics.ReportUndefinedType(syntax.Identifier.Location, syntax.Identifier.Text);
            }

            return type!;
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isReadOnly, bool isStatic, VariableKind kind, TypeSymbol type, BoundConstant? constant = null, bool addToScope = true)
        {
            var declare = !identifier.IsMissing;
            var name = declare ? identifier.Text : "?";
            var variable = VariableSymbol.New(identifier, name, isReadOnly, isStatic, kind, type, constant);

            if (declare && addToScope)
            {
                _scope.TryDeclareVariable(variable);
            }
            return variable;
        }

        private Symbol? BindSymbolReference(SyntaxToken identifier, bool report = true)
        {
            var symbol = _scope.TryLookupSymbol(identifier.Text);
            if (symbol == null && report)
            {
                Diagnostics.ReportUndefinedName(identifier.Location, identifier.Text);
            }
            return symbol;
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var thenStatement = BindStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause == null ? null : BindStatement(syntax.ElseClause.ElseStatement);
            return new BoundIfStatement(syntax, condition, thenStatement, elseStatement);
        }

        private BoundStatement BindDoWhileStatement(DoWhileStatementSyntax syntax)
        {
            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            return new BoundDoWhileStatement(syntax, condition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            return new BoundWhileStatement(syntax, condition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            _scope = new BlockBoundScope(_scope);

            var variable = BindVariableDeclaration(syntax.Identifier, false, false, VariableKind.Local, TypeSymbol.Int, null, true);

            var lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int);
            var upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int);

            var step = syntax.StepClause != null ?
                BindExpression(syntax.StepClause.Expression, TypeSymbol.Int) :
                new BoundLiteralExpression(null!, 1);

            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            _scope = _scope.Parent!;

            return new BoundForStatement(syntax, variable, lowerBound, upperBound, step, body, breakLabel, continueLabel);
        }

        private BoundStatement BindLoopBody(StatementSyntax body, out BoundLabel breakLabel, out BoundLabel continueLabel)
        {
            _labelCounter++;
            breakLabel = new BoundLabel($"break{_labelCounter}");
            continueLabel = new BoundLabel($"continue{_labelCounter}");

            _loopStack.Push((breakLabel, continueLabel));
            var boundBody = BindStatement(body);
            _loopStack.Pop();

            return boundBody;
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }
            var breakLabel = _loopStack.Peek().BreakLabel;
            return new BoundGotoStatement(syntax, breakLabel);
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }

            var continueLabel = _loopStack.Peek().ContinueLabel;
            return new BoundGotoStatement(syntax, continueLabel);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            Debug.Assert(_function != null);
            var expression = syntax.Expression == null ? null : BindExpression(syntax.Expression);

            if (_function.ReturnType == TypeSymbol.Void)
            {
                if (syntax.Expression != null)
                {
                    Diagnostics.ReportInvalidReturnExpression(syntax.Expression.Location, _function.Name);
                }
            }
            else
            {
                if (expression != null)
                {
                    expression = BindConversion(syntax.Expression!.Location, expression, _function.ReturnType, false);
                }
                else
                {
                    Diagnostics.ReportMissingReturnExpression(syntax.ReturnKeyword.Location, _function.ReturnType);
                }
            }

            return new BoundReturnStatement(syntax, expression);
        }

        private static BoundStatement BindErrorStatement(SyntaxNode syntax)
        {
            return new BoundExpressionStatement(syntax, new BoundErrorExpression(syntax));
        }

        private static BoundScope CreateRootScope()
        {
            var result = new RootBoundScope();
            foreach (var f in BuiltinFunctions.GetAll())
            {
                result.TryDeclareFunction(f);
            }
            return result;
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, bool canBeVoid = false)
        {
            var result = BindExpressionInternal(syntax);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                Diagnostics.ReportExpressionMustHaveValue(syntax.Location);
                return new BoundErrorExpression(syntax);
            }

            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax)syntax);
                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)syntax);
                case SyntaxKind.CallExpression:
                    return BindCallExpression((CallExpressionSyntax)syntax);
                case SyntaxKind.MemberAccessExpression:
                    return BindMemberAccessExpression((MemberAccessExpressionSyntax)syntax);
                case SyntaxKind.ParenthesizedExpression:
                    return BindParenthesizedExpression((ParenthesizedExpressionSyntax)syntax);
                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)syntax);
                case SyntaxKind.NameExpression:
                    return BindNameExpression((NameExpressionSyntax)syntax);
                case SyntaxKind.SelfKeyword:
                    return BindSelfKeyword((SelfKeywordSyntax)syntax);
                default:
                    throw new InvalidExpressionException($"Unexpected expression syntax {syntax.Kind}");
            }
        }

        public BoundExpression BindExpression(ExpressionSyntax expression, TypeSymbol targetType)
        {
            return BindConversion(expression, targetType, false);
        }

        private static BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;
            return new BoundLiteralExpression(syntax, value);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);

            if (boundOperand.Kind == BoundNodeKind.ErrorExpression)
            {
                return new BoundErrorExpression(syntax);
            }

            var boundOperatorKind = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);
            if (boundOperatorKind == null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundOperand.Type);
                return new BoundErrorExpression(syntax);
            }
            return new BoundUnaryExpression(syntax, boundOperand, boundOperatorKind);
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
        {
            return BindExpression(syntax.Expression);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);

            if (boundLeft.Kind == BoundNodeKind.ErrorExpression ||
                boundRight.Kind == BoundNodeKind.ErrorExpression)
            {
                return new BoundErrorExpression(syntax);
            }

            // Assignments are binary expressions, but we need treat them separatly
            if (syntax.OperatorToken.Kind.IsAssignmentOperator())
            {
                return BindAssignmentExpression(syntax, boundLeft, boundRight);
            }

            var boundOperatorKind = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (boundOperatorKind == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression(syntax);
            }
            return new BoundBinaryExpression(syntax, boundLeft, boundOperatorKind, boundRight);
        }

        private BoundExpression BindAssignmentExpression(BinaryExpressionSyntax syntax, BoundExpression boundLeft, BoundExpression boundRight)
        {
            switch (boundLeft.Kind)
            {
                case BoundNodeKind.VariableExpression:
                    return BindVariableAssignment(syntax, boundRight, (BoundVariableExpression)boundLeft);

                case BoundNodeKind.MemberAccessExpression:
                    return BindMemberAssignment(syntax, boundRight, (BoundMemberAccessExpression)boundLeft);

                default:
                    throw new InvalidOperationException($"Unexpected assignment of {boundLeft.Kind} expression");
            }
        }

        private BoundExpression BindVariableAssignment(BinaryExpressionSyntax syntax, BoundExpression boundRight, BoundVariableExpression expression)
        {
            if (expression.Variable.IsReadOnly)
            {
                Diagnostics.ReportCannotReassign(syntax.OperatorToken.Location, expression.Variable.Name);
            }

            if (syntax.OperatorToken.Kind != SyntaxKind.EqualsToken)
            {
                var equivalentOperatorTokenKind = SyntaxFacts.GetBinaryOperatorOfAssignmentOperator(syntax.OperatorToken.Kind);
                var boundAssignOperator = BoundBinaryOperator.Bind(equivalentOperatorTokenKind, expression.Type, boundRight.Type);

                if (boundAssignOperator == null)
                {
                    Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, expression.Type, boundRight.Type);
                    return new BoundErrorExpression(syntax);
                }

                var convertedExpression = BindConversion(syntax.Right.Location, boundRight, expression.Type);
                return new BoundCompoundAssignmentExpression(syntax, expression, boundAssignOperator, convertedExpression);
            }
            else
            {
                var convertedExpression = BindConversion(syntax.Right.Location, boundRight, expression.Type);
                return new BoundAssignmentExpression(syntax, expression, convertedExpression);
            }
        }

        private BoundExpression BindMemberAssignment(BinaryExpressionSyntax syntax, BoundExpression boundRight, BoundMemberAccessExpression expression)
        {
            if (expression.Member.Symbol.MemberKind == MemberKind.Method)
            {
                Diagnostics.ReportCannotAssignMethodMember(syntax.OperatorToken.Location, expression.Member.Symbol.Name, expression.Instance.Type.Name);
                return new BoundErrorExpression(syntax);
            }

            var field = (FieldSymbol)expression.Member.Symbol;
            if (field.IsReadOnly)
            {
                Diagnostics.ReportCannotReassign(syntax.OperatorToken.Location, expression.Member.Symbol.Name);
            }

            if (syntax.OperatorToken.Kind != SyntaxKind.EqualsToken)
            {
                var equivalentOperatorTokenKind = SyntaxFacts.GetBinaryOperatorOfAssignmentOperator(syntax.OperatorToken.Kind);
                var boundAssignOperator = BoundBinaryOperator.Bind(equivalentOperatorTokenKind, expression.Member.Type, boundRight.Type);

                if (boundAssignOperator == null)
                {
                    Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, expression.Member.Type, boundRight.Type);
                    return new BoundErrorExpression(syntax);
                }

                var convertedExpression = BindConversion(syntax.Right.Location, boundRight, expression.Type);
                return new BoundCompoundAssignmentExpression(syntax, expression, boundAssignOperator, convertedExpression);
            }
            else
            {
                var convertedExpression = BindConversion(syntax.Right.Location, boundRight, expression.Type);
                return new BoundAssignmentExpression(syntax, expression, convertedExpression);
            }
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax)
        {
            // All built-in basic types have a conversion function with the same name
            // that accepts one parameter.
            var type = LookupType(syntax.IdentifierToken.Text);

            if (syntax.Arguments.Count == 1 && !(type is StructSymbol) && type is TypeSymbol t)
            {
                return BindConversion(syntax.Arguments[0], t, allowExplicit: true);
            }

            var symbol = _scope.TryLookupSymbol(syntax.IdentifierToken.Text);
            if (symbol == null)
            {
                Diagnostics.ReportUndefinedFunction(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression(syntax);
            }

            if (symbol is TypeSymbol ts)
            {
                Debug.Assert(ts.BoundScope != null);
                symbol = ts.BoundScope.TryLookupSymbol<FunctionSymbol>(".ctor");
            }

            if (symbol is not FunctionSymbol function)
            {
                Diagnostics.ReportNotAFunction(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression(syntax);
            }

            return BindCallExpression(syntax, function);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, FunctionSymbol function)
        {
            var boundArgumentBuilder = ImmutableArray.CreateBuilder<BoundExpression>();
            foreach (var argument in syntax.Arguments)
            {
                var boundArgument = BindExpression(argument);
                boundArgumentBuilder.Add(boundArgument);
            }

            var parameters = function.Parameters;
            var boundArguments = boundArgumentBuilder.ToImmutable();
            var matchFunction = function.MatchArgumentsAndParameters(boundArguments);

            if (matchFunction == null && syntax.Arguments.Count != function.Parameters.Length)
            {
                TextSpan span;
                if (syntax.Arguments.Count > function.Parameters.Length)
                {
                    SyntaxNode firstExceedingNode;
                    if (function.Parameters.Length > 0)
                    {
                        firstExceedingNode = syntax.Arguments.GetSeparator(function.Parameters.Length - 1);
                    }
                    else
                    {
                        firstExceedingNode = syntax.Arguments[0];
                    }
                    var lastExceedingArgument = syntax.Arguments[^1];
                    span = TextSpan.FromBounds(firstExceedingNode.Span.Start, lastExceedingArgument.Span.End);
                }
                else
                {
                    span = syntax.CloseParenthesisToken.Span;
                }
                var location = new TextLocation(syntax.SyntaxTree.Text, span);
                Diagnostics.ReportWrongArgumentCount(location, function.Name, function.Parameters.Length, syntax.Arguments.Count);
                return new BoundErrorExpression(syntax, function.ReturnType);
            }
            else if (matchFunction == null)
            {
                var builder = new StringBuilder();
                builder.AppendJoin(", ", boundArguments.Select(a => a.Type.Name));
                var signature = $"({builder})";
                Diagnostics.ReportUndefinedOverloadForArguments(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text, signature);
            }
            else
            {
                parameters = matchFunction.Parameters;
            }

            for (var i = 0; i < syntax.Arguments.Count; i++)
            {
                var argument = boundArgumentBuilder[i];
                var parameter = parameters[i];
                var argumentLocation = syntax.Arguments[i].Location;
                boundArgumentBuilder[i] = BindConversion(argumentLocation, argument, parameter.Type, false);
            }

            if (matchFunction == null)
            {
                return new BoundErrorExpression(syntax);
            }

            boundArguments = boundArgumentBuilder.ToImmutable();
            return new BoundCallExpression(syntax, function, boundArguments);
        }

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax syntax)
        {
            var originalScope = _scope;
            try
            {
                var members = new Stack<NameExpressionSyntax>();
                var parent = syntax.ParentExpression;

                members.Push(syntax.MemberExpression);

                while (parent is MemberAccessExpressionSyntax access)
                {
                    members.Push(access.MemberExpression);
                    parent = access.ParentExpression;
                }

                BoundExpression boundParent = BindNameExpression(parent, true);
                BoundExpression receiverExpression;
                _scope = boundParent.Type.BoundScope!;

                do
                {
                    // Advance the scope trough the nested access
                    var exp = members.Pop();
                    var boundMember = BindMemberOrNestedTypeExpression(exp);

                    Debug.Assert(boundMember.Type.BoundScope != null);
                    _scope = boundMember.Type.BoundScope;

                    if (boundMember.Kind == BoundNodeKind.ErrorExpression)
                    {
                        return boundMember;
                    }
                    else if (boundMember.Kind == BoundNodeKind.TypeReferenceExpression)
                    {
                        receiverExpression = new BoundNestedTypeAccessExpression(syntax, boundParent, (BoundTypeReferenceExpression)boundMember);
                    }
                    else
                    {
                        receiverExpression = new BoundMemberAccessExpression(syntax, boundParent, (BoundMemberExpression)boundMember);
                    }
                    boundParent = receiverExpression;
                } while (members.Count > 0);
                return receiverExpression;
            }
            finally
            {
                _scope = originalScope;
            }
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false)
        {
            var expression = BindExpression(syntax);
            return BindConversion(syntax.Location, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextLocation diagnosticLocation, BoundExpression expression, TypeSymbol type, bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
                {
                    Diagnostics.ReportCannotConvert(diagnosticLocation, expression.Type, type);
                }
                return new BoundErrorExpression(expression.Syntax, type);
            }

            if (!allowExplicit && conversion.IsExplicit)
            {
                Diagnostics.ReportCannotConvertImplicitly(diagnosticLocation, expression.Type, type);
            }

            if (conversion.IsIdentity)
            {
                return expression;
            }

            return new BoundConversionExpression(expression.Syntax, type, expression);
        }

        private T? GetAncestorScope<T>() where T : class, IBoundScope
        {
            var result = _scope as T;
            var parent = _scope.Parent;
            while (result == null && parent != null)
            {
                result = parent as T;
                parent = parent.Parent;
            }
            return result;
        }

        private BoundExpression BindMemberOrNestedTypeExpression(NameExpressionSyntax syntax)
        {
            if (syntax.IdentifierToken.IsMissing)
            {
                return new BoundErrorExpression(syntax);
            }

            if (syntax.Kind == SyntaxKind.CallExpression)
            {
                return BindCallExpression((CallExpressionSyntax)syntax);
            }

            var typeScope = GetAncestorScope<TypeBoundScope>();
            Debug.Assert(typeScope?.OwnerType != null);

            var symbol = BindSymbolReference(syntax.IdentifierToken, false);
            if (symbol == null)
            {
                Diagnostics.ReportCannotAccessMember(syntax.Location, typeScope.OwnerType.Name, syntax.IdentifierToken.Text);
                return new BoundErrorExpression(syntax);
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Member:
                case SymbolKind.Function: // TODO - HACK: Functions should be members
                    return new BoundMemberExpression(syntax, (MemberSymbol)symbol);

                case SymbolKind.Type:
                case SymbolKind.Enum:
                case SymbolKind.Struct:
                    return new BoundTypeReferenceExpression(syntax, (TypeSymbol)symbol);

                default:
                    throw new InvalidOperationException($"Unexpected symbol of kind {symbol.Kind}");
            }
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax, bool byReference = false)
        {
            if (syntax.IdentifierToken.IsMissing)
            {
                // This means the token was inserted by the parser.
                // An error has already been reported so we can just return an error expression.
                return new BoundErrorExpression(syntax);
            }

            if (syntax.IdentifierToken.Kind == SyntaxKind.SelfKeyword)
            {
                return BindSelfKeyword((SelfKeywordSyntax)syntax);
            }

            var symbol = BindSymbolReference(syntax.IdentifierToken);
            if (symbol == null)
            {
                // No need to report an error
                // BindSymbolReference already reported
                return new BoundErrorExpression(syntax);
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Variable:
                    return new BoundVariableExpression(syntax, (VariableSymbol)symbol, byReference);

                case SymbolKind.Member:
                    // If we get here, means an access to a member
                    // without the use of the SelfKeyword.
                    // So we synthesize a syntax with the self,
                    // and pass to the apropriate method
                    var keywordToken = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.SelfKeyword, -1, SyntaxKind.SelfKeyword.GetText(), null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
                    var selfExpression = new SelfKeywordSyntax(syntax.SyntaxTree, keywordToken);
                    var dotToken = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.DotToken, -1, SyntaxKind.DotToken.GetText(), null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
                    var accessSyntax = new MemberAccessExpressionSyntax(syntax.SyntaxTree,
                    selfExpression, dotToken, syntax);
                    return BindMemberAccessExpression(accessSyntax);

                case SymbolKind.Type:
                case SymbolKind.Enum:
                case SymbolKind.Struct:
                    return new BoundTypeReferenceExpression(syntax, (TypeSymbol)symbol);

                default:
                    Diagnostics.ReportNotAVariable(syntax.Location, syntax.IdentifierToken.Text);
                    return new BoundErrorExpression(syntax);
            }
        }

        private BoundExpression BindSelfKeyword(SelfKeywordSyntax syntax)
        {
            if (_function == null)
            {
                Diagnostics.ReportCannotUseSelfOutsideOfAFunction(syntax.IdentifierToken.Location);
                return new BoundErrorExpression(syntax);
            }

            if (_function.ReceiverType == null)
            {
                Diagnostics.ReportCannotUseSelfOutsideOfReceiverFunctions(syntax.IdentifierToken.Location, _function.Name);
                return new BoundErrorExpression(syntax);
            }

            return new BoundSelfExpression(syntax, _function.ReceiverType);
        }

        private TypeSymbol? LookupType(string name)
        {
            var type = TypeSymbol.LookupType(name);
            if (type != null)
            {
                return type;
            }

            var maybeSymbol = _scope.TryLookupSymbol(name);
            if (maybeSymbol is TypeSymbol s)
            {
                return s;
            }
            return null;
        }
    }
}
