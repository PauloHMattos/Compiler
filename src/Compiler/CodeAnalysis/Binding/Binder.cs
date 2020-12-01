using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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
        private BoundScope _scope;
        private readonly FunctionSymbol? _function;
        private readonly Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _loopStack;

        public DiagnosticBag Diagnostics { get; }

        private Binder(BoundScope? parent, FunctionSymbol? function)
        {
            _scope = new BoundScope(parent);
            Diagnostics = new DiagnosticBag();
            _function = function;
            _loopStack = new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();

            if (function != null)
            {
                foreach (var p in function.Parameters)
                {
                    _ = _scope.TryDeclareVariable(p);
                }
            }
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, ImmutableArray<SyntaxTree> syntaxTrees)
        {
            var parentScope = CreateParentScope(previous);
            var binder = new Binder(parentScope, null);

            binder.Diagnostics.AddRange(syntaxTrees.SelectMany(st => st.Diagnostics));
            if (binder.Diagnostics.HasErrors())
            {
                return new BoundGlobalScope(previous,
                    binder.Diagnostics.ToImmutableArray(),
                    null,
                    ImmutableArray<FunctionSymbol>.Empty,
                    ImmutableArray<EnumSymbol>.Empty,
                    ImmutableArray<StructSymbol>.Empty,
                    ImmutableArray<VariableSymbol>.Empty,
                    ImmutableArray<BoundStatement>.Empty);
            }


            var enumDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<EnumDeclarationSyntax>();

            foreach (var enumDeclaration in enumDeclarations)
            {
                binder.BindEnumDeclaration(enumDeclaration);
            }


            var structDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<StructDeclarationSyntax>();

            foreach (var structDeclaration in structDeclarations)
            {
                binder.BindStructDeclaration(structDeclaration);
            }

            var functionDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<FunctionDeclarationSyntax>();

            foreach (var function in functionDeclarations)
            {
                binder.BindFunctionDeclaration(function);
            }

            var globalStatements = syntaxTrees
                                    .SelectMany(st => st.Root.Members)
                                    .OfType<GlobalStatementSyntax>();
            var statements = ImmutableArray.CreateBuilder<BoundStatement>();

            foreach (var globalStatement in globalStatements)
            {
                var s = binder.BindGlobalStatement(globalStatement.Statement);
                statements.Add(s);
            }
            // Check global statements

            var firstGlobalStatementPerSyntaxTree = syntaxTrees
                        .Select(st => st.Root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault())
                        .Where(g => g != null)
                        .Select(g => g!)
                        .ToArray();

            if (firstGlobalStatementPerSyntaxTree.Length > 1)
            {
                foreach (var globalStatement in firstGlobalStatementPerSyntaxTree)
                {
                    binder.Diagnostics.ReportOnlyOneFileCanHaveGlobalStatements(globalStatement.Location);
                }
            }

            // Check for main/script with global statements
            var functions = binder._scope.GetDeclaredFunctions();

            FunctionSymbol? mainFunction;

            mainFunction = functions.FirstOrDefault(f => f.Name == "main");
            if (mainFunction != null && (mainFunction.ReturnType != TypeSymbol.Void || mainFunction.Parameters.Any()))
            {
                binder.Diagnostics.ReportMainMustHaveCorrectSignature(mainFunction.Declaration!.Identifier.Location);
            }

            if (globalStatements.Any())
            {
                if (mainFunction != null)
                {
                    binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(mainFunction.Declaration!.Identifier.Location);

                    foreach (var globalStatement in firstGlobalStatementPerSyntaxTree)
                    {
                        binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(globalStatement.Location);
                    }
                }
                else
                {
                    mainFunction = new FunctionSymbol("main", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, null);
                }
            }

            var diagnostics = binder.Diagnostics.ToImmutableArray();
            var variables = binder._scope.GetDeclaredVariables();
            var enums = binder._scope.GetDeclaredEnums();
            var structs = binder._scope.GetDeclaredStructs();

            if (previous != null)
            {
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
            }

            return new BoundGlobalScope(previous, diagnostics, mainFunction, functions, enums, structs, variables, statements.ToImmutable());
        }

        public static BoundProgram BindProgram(BoundProgram? previous, BoundGlobalScope globalScope)
        {
            var parentScope = CreateParentScope(globalScope);
            if (globalScope.Diagnostics.HasErrors())
            {
                return new BoundProgram(previous,
                    globalScope.Diagnostics,
                    null,
                    ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Empty,
                    ImmutableArray<EnumSymbol>.Empty,
                    ImmutableDictionary<StructSymbol, BoundBlockStatement>.Empty);
            }

            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            var structBodies = ImmutableDictionary.CreateBuilder<StructSymbol, BoundBlockStatement>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            foreach (var structSymbol in globalScope.Structs)
            {
                var binder = new Binder(parentScope, null);
                var body = binder.BindMemberBlockStatement(structSymbol.Declaration!.Body);
                var loweredBody = Lowerer.Lower(structSymbol, body);

                structBodies.Add(structSymbol, loweredBody);

                diagnostics.AddRange(binder.Diagnostics);
            }

            foreach (var function in globalScope.Functions)
            {
                // Structs generate declartions for their constructors.  However, these have no function bodies.
                // We will skip attempting to lower the bodies for these and allow the Emitter to automatically
                // generate the code necessary.  This will avoid the potential of reporting diagnostic errors to
                // the user for code they never wrote.
                if (function.ReturnType is StructSymbol && function.Name.EndsWith(".ctor"))
                {
                    continue;
                }

                var binder = new Binder(parentScope, function);
                var body = binder.BindStatement(function.Declaration!.Body);
                var loweredBody = Lowerer.Lower(function, body);

                if (function.ReturnType != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                {
                    binder.Diagnostics.ReportAllPathsMustReturn(function.Declaration!.Identifier.Location);
                }

                functionBodies.Add(function, loweredBody);

                diagnostics.AddRange(binder.Diagnostics);
            }

            var compilationUnit = globalScope.Statements.Any()
                                    ? globalScope.Statements.First().Syntax.AncestorsAndSelf().LastOrDefault()
                                    : null;

            if (globalScope.MainFunction != null && globalScope.Statements.Any())
            {
                var body = Lowerer.Lower(globalScope.MainFunction, new BoundBlockStatement(compilationUnit!, globalScope.Statements));
                functionBodies.Add(globalScope.MainFunction, body);
            }

            return new BoundProgram(previous,
                                    diagnostics.ToImmutable(),
                                    globalScope.MainFunction,
                                    functionBodies.ToImmutable(),
                                    globalScope.Enums,
                                    structBodies.ToImmutable());
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
        {
            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

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
                    var parameter = new ParameterSymbol(parameterName, parameterType, parameters.Count);
                    parameters.Add(parameter);
                }
            }

            var type = BindTypeClause(syntax.Type) ?? TypeSymbol.Void;
            var receiver = BindTypeClause(syntax.Receiver);

            var function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, ImmutableArray<FunctionSymbol>.Empty, syntax, receiver);
            if (function.Name != null && !_scope.TryDeclareFunction(function))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, function.Name);
            }
        }

        private void BindEnumDeclaration(EnumDeclarationSyntax syntax)
        {
            var enumValues = ImmutableArray.CreateBuilder<(string Name, int Value)>();

            var seenValueNames = new HashSet<string>();
            var seenValues = new Dictionary<int, string>();

            foreach (var enumValueSyntax in syntax.Values)
            {
                var valueName = enumValueSyntax.Identifier.Text;
                if (!seenValueNames.Add(valueName))
                {
                    Diagnostics.ReportMemberAlreadyDeclared(enumValueSyntax.Location, syntax.Identifier.Text, valueName);
                }
                else
                {
                    int value = 0;
                    if (enumValueSyntax.ValueClause != null)
                    {
                        var boundValueExpression = BindExpression(enumValueSyntax.ValueClause.Expression);
                        value = (int)boundValueExpression.ConstantValue!.Value;
                    }
                    else if (enumValues.Count != 0)
                    {
                        var lastEnumValue = enumValues.Last();
                        value = lastEnumValue!.Value + 1;
                    }

                    if (seenValues.TryGetValue(value, out var otherName))
                    {
                        Diagnostics.ReportEnumerationAlreadyContainsValue(enumValueSyntax.Location, valueName, value, otherName!);
                    }
                    else
                    {
                        seenValues.Add(value, valueName);
                    }
                    enumValues.Add((valueName, value));
                }
            }

            string enumIdentifier = syntax.Identifier.Text;
            var enumSymbol = new EnumSymbol(enumIdentifier, enumValues.ToImmutable(), syntax);
            if (enumIdentifier != null && !_scope.TryDeclareEnum(enumSymbol))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, enumSymbol.Name);
            }
        }

        private void BindStructDeclaration(StructDeclarationSyntax syntax)
        {
            // Peek into the struct body and generate a constructor based on all writeable members
            var members = syntax.Body.Statement.OfType<VariableDeclarationStatementSyntax>()
                                               .Where(m => m.Keyword.Kind == SyntaxKind.VarKeyword);

            var ctorParameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
            var boundMembers = ImmutableArray.CreateBuilder<MemberSymbol>();

            foreach (var varDeclarationSyntax in members)
            {
                var decl = BindVariableDeclarationStatement(varDeclarationSyntax, false);

                if (decl is BoundVariableDeclarationStatement d)
                {
                    var field = new FieldSymbol(d);
                    boundMembers.Add(field);
                }

                if (varDeclarationSyntax.Keyword.Kind == SyntaxKind.ConstKeyword)
                {
                    // These are not candidates for ctorParameters because they are read-only
                    continue;
                }

                var parameterName = varDeclarationSyntax.Identifier.Text;
                var parameterType = BindTypeClause(varDeclarationSyntax.TypeClause);

                if (parameterType == null && varDeclarationSyntax.Initializer != null)
                {
                    parameterType = BindExpression(varDeclarationSyntax.Initializer).Type;
                }
                else if (parameterType == null && varDeclarationSyntax.Initializer == null)
                {
                    // We don't need to do error reporting here (e.g. report on duplicate members) because that will be
                    // done later in the BindMemberBlockStatement
                    continue;
                }

                var parameter = new ParameterSymbol(parameterName, parameterType!, ctorParameters.Count);

                ctorParameters.Add(parameter);
            }

            string structIdentifier = syntax.Identifier.Text;
            var structSymbol = new StructSymbol(structIdentifier, ctorParameters.ToImmutable(), boundMembers.ToImmutable(), syntax);
            if (structIdentifier != null && !_scope.TryDeclareStruct(structSymbol))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, structSymbol.Name);
            }

            // Declare Built-in Constructor
            var ctor = new FunctionSymbol(structIdentifier + ".ctor", ImmutableArray<ParameterSymbol>.Empty, structSymbol);
            var ctorWithParams = new FunctionSymbol(structIdentifier + ".ctor", ctorParameters.ToImmutable(), structSymbol);
            if (structIdentifier != null)
            {
                if (!_scope.TryDeclareFunction(ctor))
                {
                    Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, ctor.Name);
                }
                if (!_scope.TryDeclareFunction(ctorWithParams))
                {
                    Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, ctorWithParams.Name);
                }
            }
        }

        private BoundStatement BindMemberBlockStatement(MemberBlockStatementSyntax syntax)
        {
            var statements = ImmutableArray.CreateBuilder<BoundStatement>();
            _scope = new BoundScope(_scope);

            foreach (var statementSyntax in syntax.Statement)
            {
                if (statementSyntax.Kind != SyntaxKind.VariableDeclarationStatement)
                {
                    Diagnostics.ReportInvalidExpressionStatement(statementSyntax.Location);
                }

                var statement = BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)statementSyntax);
                statements.Add(statement);
            }

            return new BoundMemberBlockStatement(syntax, statements.ToImmutable());
        }

        private BoundStatement BindGlobalStatement(StatementSyntax syntax)
        {
            return BindStatement(syntax, isGlobal: true);
        }

        private BoundStatement BindStatement(StatementSyntax syntax, bool isGlobal = false)
        {
            var result = BindStatementInternal(syntax);
            if (!isGlobal && result is BoundExpressionStatement es)
            {
                var isAllowedExpression = es.Expression.Kind == BoundNodeKind.ErrorExpression ||
                                            es.Expression.Kind == BoundNodeKind.CallExpression ||
                                            es.Expression.Kind == BoundNodeKind.AssignmentExpression ||
                                            es.Expression.Kind == BoundNodeKind.CompoundAssignmentExpression ||
                                            es.Expression.Kind == BoundNodeKind.MemberAssignmentExpression ||
                                            es.Expression.Kind == BoundNodeKind.CompoundMemberAssignmentExpression;

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
            _scope = new BoundScope(_scope);

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
                var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, variableType, initializer.ConstantValue, addToScope);
                var convertedInitializer = BindConversion(syntax.Initializer.Location, initializer, variableType);

                return new BoundVariableDeclarationStatement(syntax, variable, convertedInitializer);
            }
            else if (type != null)
            {
                var initializer = syntax.Initializer?.Kind == SyntaxKind.DefaultKeyword
                    ? BindDefaultExpression((DefaultKeywordSyntax)syntax.Initializer, syntax.TypeClause)
                    : BindSyntheticDefaultExpression(syntax, syntax.TypeClause);
                var variableType = type;
                var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, variableType, null, addToScope);
                var convertedInitializer = BindConversion(syntax.TypeClause!.Location, initializer!, variableType);

                return new BoundVariableDeclarationStatement(syntax, variable, convertedInitializer);
            }
            else
            {
                Diagnostics.ReportUndefinedType(syntax.TypeClause?.Location ?? syntax.Identifier.Location, syntax.TypeClause?.Identifier.Text ?? syntax.Identifier.Text);
                return BindErrorStatement(syntax);
            }
        }

        [return: NotNullIfNotNull("typeSyntax")]
        private BoundExpression? BindSyntheticDefaultExpression(VariableDeclarationStatementSyntax syntax, TypeClauseSyntax? typeSyntax)
        {
            var syntaxToken = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.DefaultKeyword, syntax.Span.End, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
            var syntaxNode = new DefaultKeywordSyntax(syntax.SyntaxTree, syntaxToken);

            return BindDefaultExpression(syntaxNode, typeSyntax);
        }

        [return: NotNullIfNotNull("typeSyntax")]
        private BoundExpression? BindDefaultExpression(DefaultKeywordSyntax syntax, TypeClauseSyntax? typeSyntax)
        {
            if (typeSyntax == null)
            {
                return null;
            }

            var type = LookupType(typeSyntax.Identifier.Text);
            if (type == null)
            {
                Diagnostics.ReportUndefinedType(typeSyntax.Identifier.Location, typeSyntax.Identifier.Text);
                type = TypeSymbol.Error;
            }

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

        private TypeSymbol? BindTypeClause(SyntaxToken? identifier)
        {
            if (identifier == null || identifier.IsMissing)
            {
                return null;
            }

            var type = LookupType(identifier.Text);
            if (type == null)
            {
                Diagnostics.ReportUndefinedType(identifier.Location, identifier.Text);
            }
            return type;
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isReadOnly, TypeSymbol type, BoundConstant? constant = null, bool addToScope = true)
        {
            var declare = !identifier.IsMissing;
            var name = declare ? identifier.Text : "?";
            var variable = _function == null
                ? (VariableSymbol)new GlobalVariableSymbol(name, isReadOnly, type, constant)
                : new LocalVariableSymbol(name, isReadOnly, type, constant);

            if (declare && addToScope && !_scope.TryDeclareVariable(variable))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);
            }

            return variable;
        }

        private Symbol? BindSymbolReference(SyntaxToken identifier, TextLocation location)
        {
            switch (_scope.TryLookupSymbol(identifier.Text))
            {
                case VariableSymbol variable:
                    return variable;

                case TypeSymbol symbol:
                    return symbol;

                case FunctionSymbol function:
                    return function;

                default:
                    if (_function?.Receiver != null)
                    {
                        return BindMemberReference(_function.Receiver, identifier, false);
                    }
                    
                    Diagnostics.ReportUndefinedName(location, identifier.Text);
                    return null;
            }
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
            _scope = new BoundScope(_scope);

            var variable = BindVariableDeclaration(syntax.Identifier, false, TypeSymbol.Int, null, true);

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
            var expression = syntax.Expression == null ? null : BindExpression(syntax.Expression);

            if (_function == null)
            {
                if (expression != null)
                {
                    // Main does not support return values.
                    Diagnostics.ReportInvalidReturnWithValueInGlobalStatements(syntax.Expression!.Location);
                }
            }
            else
            {
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
            }

            return new BoundReturnStatement(syntax, expression);
        }

        private BoundStatement BindErrorStatement(SyntaxNode syntax)
        {
            return new BoundExpressionStatement(syntax, new BoundErrorExpression(syntax));
        }

        private static BoundScope CreateParentScope(BoundGlobalScope? previous)
        {
            var stack = new Stack<BoundGlobalScope>();

            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            var parent = CreateRootScope();
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);

                foreach (var e in previous.Enums)
                {
                    scope.TryDeclareEnum(e);
                }

                foreach (var s in previous.Structs)
                {
                    scope.TryDeclareStruct(s);
                }

                foreach (var f in previous.Functions)
                {
                    scope.TryDeclareFunction(f);
                }

                foreach (var variable in previous.Variables)
                {
                    scope.TryDeclareVariable(variable);
                }
                parent = scope;
            }
            return parent;
        }

        private static BoundScope CreateRootScope()
        {
            var result = new BoundScope(null);
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

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
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

        private BoundExpression BindVariableAssignment(BinaryExpressionSyntax syntax, BoundExpression boundRight, BoundVariableExpression variable)
        {
            if (variable.Variable.IsReadOnly)
            {
                Diagnostics.ReportCannotReassign(syntax.OperatorToken.Location, variable.Variable.Name);
            }

            if (syntax.OperatorToken.Kind != SyntaxKind.EqualsToken)
            {
                var equivalentOperatorTokenKind = SyntaxFacts.GetBinaryOperatorOfAssignmentOperator(syntax.OperatorToken.Kind);
                var boundAssignOperator = BoundBinaryOperator.Bind(equivalentOperatorTokenKind, variable.Type, boundRight.Type);

                if (boundAssignOperator == null)
                {
                    Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, variable.Type, boundRight.Type);
                    return new BoundErrorExpression(syntax);
                }

                var convertedExpression = BindConversion(syntax.Right.Location, boundRight, variable.Type);
                return new BoundCompoundAssignmentExpression(syntax, variable.Variable, boundAssignOperator, convertedExpression);
            }
            else
            {
                var convertedExpression = BindConversion(syntax.Right.Location, boundRight, variable.Type);
                return new BoundAssignmentExpression(syntax, variable.Variable, convertedExpression);
            }
        }

        private BoundExpression BindMemberAssignment(BinaryExpressionSyntax syntax, BoundExpression boundRight, BoundMemberAccessExpression expression)
        {
            if (expression.Member.MemberKind == MemberKind.Method)
            {
                Diagnostics.ReportCannotAssignMethodMember(syntax.OperatorToken.Location, expression.Member.Name);
                return new BoundErrorExpression(syntax);
            }

            if (expression.Member.IsReadOnly)
            {
                Diagnostics.ReportCannotReassign(syntax.OperatorToken.Location, expression.Member.Name);
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
                return new BoundCompoundMemberAssignmentExpression(syntax, expression.Instance, expression.Member, boundAssignOperator, convertedExpression);
            }
            else
            {
                var convertedExpression = BindConversion(syntax.Right.Location, boundRight, expression.Type);
                return new BoundMemberAssignmentExpression(syntax, expression.Instance, expression.Member, convertedExpression);
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

            if (symbol is StructSymbol)
            {
                symbol = _scope.TryLookupSymbol(syntax.IdentifierToken.Text + ".ctor");
            }

            if (symbol is not FunctionSymbol function)
            {
                Diagnostics.ReportNotAFunction(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression(syntax);
            }

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
                return new BoundErrorExpression(syntax);
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


        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, BoundExpression memberAccessExpression)
        {
            var callExpression = (BoundCallExpression)BindCallExpression(syntax); 
            var function = callExpression.Function;
            var boundArguments = callExpression.Arguments;
            return memberAccessExpression switch
            {
                BoundSelfExpression i => new BoundCallExpression(syntax, i, function, boundArguments),
                BoundVariableExpression i => new BoundCallExpression(syntax, i, function, boundArguments),
                BoundMemberAccessExpression i => new BoundCallExpression(syntax, i, function, boundArguments),
                _ => new BoundErrorExpression(syntax)
            };
        }

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax syntax)
        {
            if (syntax.ParentExpression.Kind == SyntaxKind.MemberAccessExpression)
            {
                return BindMemberAccessExpression((MemberAccessExpressionSyntax)syntax.ParentExpression);
            }
            else if (syntax.ParentExpression.Kind == SyntaxKind.NameExpression || 
                     syntax.ParentExpression.Kind == SyntaxKind.SelfKeyword)
            {
                var nameExpression = BindNameExpression((NameExpressionSyntax)syntax.ParentExpression, true);
                if (syntax.CallExpression)
                {
                    var func = BindFunctionReference(syntax.IdentifierToken);
                    if (func != null)
                    {
                        return BindCallExpression((CallExpressionSyntax)syntax.MemberExpression, nameExpression);
                    }
                    return new BoundErrorExpression(syntax);
                }
                else
                {
                    var member = BindMemberReference(nameExpression.Type, syntax.IdentifierToken);
                    if (member != null)
                    {
                        return new BoundMemberAccessExpression(syntax, nameExpression, member);
                    }
                    return new BoundErrorExpression(syntax);
                }
            }
            else
            {
                Diagnostics.ReportCannotAccessMember(syntax.ParentExpression.Location, syntax.ParentExpression.ToString(), "");
            }
            return new BoundErrorExpression(syntax);
        }

        private MemberSymbol? BindMemberReference(TypeSymbol typeSymbol, SyntaxToken identifierToken, bool report = true)
        {
            var memberName = identifierToken.Text;
            foreach (var member in typeSymbol.Members)
            {
                if (member.Name == memberName)
                {
                    return member;
                }
            }
            if (report)
            {
                Diagnostics.ReportCannotAccessMember(identifierToken.Location, typeSymbol.Name, identifierToken.Text);
            }
            return null;
        }

        private FunctionSymbol? BindFunctionReference(SyntaxToken identifierToken)
        {
            var name = identifierToken.Text;

            switch (_scope.TryLookupSymbol(name))
            {
                case FunctionSymbol func:
                    return func;

                default:
                    Diagnostics.ReportUndefinedFunction(identifierToken.Location, name);
                    return null;
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
                return new BoundErrorExpression(expression.Syntax);
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
            
            var symbol = BindSymbolReference(syntax.IdentifierToken, syntax.IdentifierToken.Location);
            if (symbol == null)
            {
                // No need to report an error
                // BindSymbolReference already reported
                return new BoundErrorExpression(syntax);
            }

            switch (symbol.Kind)
            {
                case SymbolKind.LocalVariable:
                case SymbolKind.GlobalVariable:
                case SymbolKind.Parameter:
                    return new BoundVariableExpression(syntax, (VariableSymbol)symbol, byReference);


                case SymbolKind.Member:
                    var selfKeyword = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.SelfKeyword, syntax.Span.End, "self", null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
                    var injectedSelf = new SelfKeywordSyntax(syntax.SyntaxTree, selfKeyword);
                    var dotToken = new SyntaxToken(syntax.SyntaxTree, SyntaxKind.DotToken, syntax.Span.End, ".", null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
                    var memberAccess = new MemberAccessExpressionSyntax(syntax.SyntaxTree, injectedSelf, dotToken, syntax);
                    return BindMemberAccessExpression(memberAccess);

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

            if (_function.Receiver == null)
            {
                Diagnostics.ReportCannotUseSelfOutsideOfReceiverFunctions(syntax.IdentifierToken.Location, _function.Name);
                return new BoundErrorExpression(syntax);
            }

            return new BoundSelfExpression(syntax, _function.Receiver);
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
