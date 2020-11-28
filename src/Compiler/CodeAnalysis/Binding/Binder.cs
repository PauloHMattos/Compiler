﻿using System;
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
                    ImmutableArray<VariableSymbol>.Empty,
                    ImmutableArray<BoundStatement>.Empty);
            }

            var functionDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<FunctionDeclarationSyntax>();

            foreach (var function in functionDeclarations)
            {
                binder.BindFunctionDeclaration(function);
            }


            var enumDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<EnumDeclarationSyntax>();

            foreach (var enumDeclaration in enumDeclarations)
            {
                binder.BindEnumDeclaration(enumDeclaration);
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
            if (mainFunction != null && (mainFunction.Type != TypeSymbol.Void || mainFunction.Parameters.Any()))
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

            if (previous != null)
            {
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
            }

            return new BoundGlobalScope(previous, diagnostics, mainFunction, functions, enums, variables, statements.ToImmutable());
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
                    ImmutableArray<EnumSymbol>.Empty);
            }
            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            foreach (var function in globalScope.Functions)
            {
                var binder = new Binder(parentScope, function);
                var body = binder.BindStatement(function.Declaration!.Body);
                var loweredBody = Lowerer.Lower(function, body);

                if (function.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
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
                                    globalScope.Enums);
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

            var function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax);
            if (syntax.Identifier.Text != null && !_scope.TryDeclareFunction(function))
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

        private BoundStatement BindGlobalStatement(StatementSyntax syntax)
        {
            return BindStatement(syntax, isGlobal: true);
        }

        private BoundStatement BindStatement(StatementSyntax syntax, bool isGlobal = false)
        {
            var result = BindStatementInternal(syntax);
            if (!isGlobal)
            {
                if (result is BoundExpressionStatement es)
                {
                    var isAllowedExpression = es.Expression.Kind == BoundNodeKind.ErrorExpression ||
                                              es.Expression.Kind == BoundNodeKind.AssignmentExpression ||
                                              es.Expression.Kind == BoundNodeKind.CallExpression ||
                                              es.Expression.Kind == BoundNodeKind.CompoundAssignmentExpression;
                    if (!isAllowedExpression)
                    {
                        Diagnostics.ReportInvalidExpressionStatement(syntax.Location);
                    }
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

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            var isReadOnly = syntax.Keyword.Kind == SyntaxKind.ConstKeyword;
            var type = BindTypeClause(syntax.TypeClause);

            if (syntax.Initializer != null && syntax.Initializer.Kind != SyntaxKind.DefaultKeyword)
            {
                var initializer = BindExpression(syntax.Initializer);
                var variableType = type ?? initializer.Type;
                var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, variableType, initializer.ConstantValue);
                var convertedInitializer = BindConversion(syntax.Initializer.Location, initializer, variableType);

                return new BoundVariableDeclarationStatement(syntax, variable, convertedInitializer);
            }
            else if (type != null)
            {
                var initializer = syntax.Initializer?.Kind == SyntaxKind.DefaultKeyword
                    ? BindDefaultExpression((DefaultKeywordSyntax)syntax.Initializer, syntax.TypeClause)
                    : BindSyntheticDefaultExpression(syntax, syntax.TypeClause);
                var variableType = type;
                var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, variableType);
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

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isReadOnly, TypeSymbol type, BoundConstant? constant = null)
        {
            var declare = !identifier.IsMissing;
            var name = declare ? identifier.Text : "?";
            var variable = _function == null
                ? (VariableSymbol)new GlobalVariableSymbol(name, isReadOnly, type, constant)
                : new LocalVariableSymbol(name, isReadOnly, type, constant);

            if (declare && !_scope.TryDeclareVariable(variable))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);
            }

            return variable;
        }

        private Symbol? BindSymbolReference(string name, TextLocation location)
        {
            switch (_scope.TryLookupSymbol(name))
            {
                case VariableSymbol variable:
                    return variable;

                case TypeSymbol symbol:
                    return symbol;

                case FunctionSymbol function:
                    return function;

                default:
                    Diagnostics.ReportUndefinedName(location, name);
                    return null;
            }
        }

        private T? BindSymbolReference<T>(string name, TextLocation location, Action<TextLocation, string> reportDelegate) where T : Symbol
        {
            var symbol = BindSymbolReference(name, location);
            if (symbol == null)
            {
                return null;
            }
            if (symbol is T s)
            {
                return s;
            }
            reportDelegate.Invoke(location, name);
            return null;
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

            var variable = BindVariableDeclaration(syntax.Identifier, false, TypeSymbol.Int);

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
                if (_function.Type == TypeSymbol.Void)
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
                        expression = BindConversion(syntax.Expression!.Location, expression, _function.Type, false);
                    }
                    else
                    {
                        Diagnostics.ReportMissingReturnExpression(syntax.ReturnKeyword.Location, _function.Type);
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

                foreach (var f in previous.Functions)
                {
                    scope.TryDeclareFunction(f);
                }

                foreach (var e in previous.Enums)
                {
                    scope.TryDeclareEnum(e);
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
                case SyntaxKind.AssignmentExpression:
                    return BindAssignmentExpression((AssignmentExpressionSyntax)syntax);
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

            var boundOperatorKind = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (boundOperatorKind == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression(syntax);
            }
            return new BoundBinaryExpression(syntax, boundLeft, boundOperatorKind, boundRight);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax)
        {
            // All built-in basic types have a conversion function with the same name
            // that accepts one parameter.
            var type = LookupType(syntax.IdentifierToken.Text);

            if (syntax.Arguments.Count == 1 && type is TypeSymbol t)
            {
                return BindConversion(syntax.Arguments[0], t, allowExplicit: true);
            }

            var symbol = _scope.TryLookupSymbol(syntax.IdentifierToken.Text);
            if (symbol == null)
            {
                Diagnostics.ReportUndefinedFunction(syntax.IdentifierToken.Location, syntax.IdentifierToken.Text);
                return new BoundErrorExpression(syntax);
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

            return new BoundCallExpression(syntax, matchFunction, boundArgumentBuilder.ToImmutable());
        }

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax syntax)
        {
            if (syntax.ParentExpression.Kind == SyntaxKind.NameExpression)
            {
                var nameExpression = BindExpression(syntax.ParentExpression);
                var variable = BindMemberReference(nameExpression, syntax.MemberExpression);
                if (variable == null)
                {
                    return new BoundErrorExpression(syntax);
                }

                return new BoundMemberAccessExpression(syntax, nameExpression, variable);
            }
            else if (syntax.ParentExpression.Kind == SyntaxKind.MemberAccessExpression)
            {
                var nameExpression = BindExpression(syntax.ParentExpression);
                var member = BindMemberReference(nameExpression, syntax.MemberExpression);
                if (member == null)
                {
                    return new BoundErrorExpression(syntax);
                }

                return new BoundMemberAccessExpression(syntax, nameExpression, member);
            }
            else
            {
                Diagnostics.ReportCannotAccessMember(syntax.ParentExpression.Location, syntax.ParentExpression.ToString(), "");
                return new BoundErrorExpression(syntax);
            }
        }

        private MemberSymbol? BindMemberReference(BoundExpression variable, NameExpressionSyntax memberExpression)
        {
            return BindMemberReference(variable.Type, memberExpression);
        }

        private MemberSymbol? BindMemberReference(TypeSymbol typeSymbol, NameExpressionSyntax memberExpression)
        {
            var memberName = memberExpression.IdentifierToken.Text;
            if (typeSymbol is EnumSymbol e)
            {
                if (memberExpression is CallExpressionSyntax call)
                {
                    Diagnostics.ReportUndefinedFunction(call.Location, memberName);
                    return null;
                }

                foreach (var member in e.Values)
                {
                    if (member.Name == memberName)
                    {
                        return member;
                    }
                }
            }

            var typeName = typeSymbol.Name;
            Diagnostics.ReportCannotAccessMember(memberExpression.Location, typeName, memberName);
            return null;
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

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            if (syntax.IdentifierToken.IsMissing)
            {
                // This means the token was inserted by the parser.
                // An error has already been reported so we can just return an error expression.
                return new BoundErrorExpression(syntax);
            }

            var name = syntax.IdentifierToken.Text;
            var symbol = BindSymbolReference(name, syntax.IdentifierToken.Location);
            if (symbol == null)
            {
                // No need to report an error
                // BindSymbolReference already reported
                return new BoundErrorExpression(syntax);
            }

            switch (symbol)
            {
                case VariableSymbol variable:
                    return new BoundVariableExpression(syntax, variable);
                case EnumSymbol enumSymbol:
                    return new BoundTypeReferenceExpression(syntax, enumSymbol);
                default:
                    throw new InvalidOperationException();
            }
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            var variable = BindSymbolReference<VariableSymbol>(name, syntax.IdentifierToken.Location,
                    (location, name) => Diagnostics.ReportNotAVariable(location, name));
            if (variable == null)
            {
                return boundExpression;
            }

            if (variable.IsReadOnly)
            {
                Diagnostics.ReportCannotReassigned(syntax.AssignmentToken.Location, name);
            }

            if (syntax.AssignmentToken.Kind != SyntaxKind.EqualsToken)
            {
                var equivalentOperatorTokenKind = SyntaxFacts.GetBinaryOperatorOfAssignmentOperator(syntax.AssignmentToken.Kind);
                var boundOperator = BoundBinaryOperator.Bind(equivalentOperatorTokenKind, variable.Type, boundExpression.Type);

                if (boundOperator == null)
                {
                    Diagnostics.ReportUndefinedBinaryOperator(syntax.AssignmentToken.Location, syntax.AssignmentToken.Text, variable.Type, boundExpression.Type);
                    return new BoundErrorExpression(syntax);
                }
                var convertedExpression = BindConversion(syntax.Expression.Location, boundExpression, variable.Type, false);
                return new BoundCompoundAssignmentExpression(syntax, variable, boundOperator, convertedExpression);
            }
            else
            {
                var convertedExpression = BindConversion(syntax.Expression.Location, boundExpression, variable.Type, false);
                return new BoundAssignmentExpression(syntax, variable, convertedExpression);
            }
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