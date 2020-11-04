using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Lowering;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        private BoundScope _scope;
        private readonly FunctionSymbol _function;

        public DiagnosticBag Diagnostics { get; }

        private Binder(BoundScope parent, FunctionSymbol function)
        {
            _scope = new BoundScope(parent);
            Diagnostics = new DiagnosticBag();
            _function = function;

            if (function != null)
            {
                foreach (var p in function.Parameters)
                {
                    _scope.TryDeclareVariable(p);
                }
            }
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope previous, CompilationUnitSyntax compilationUnit)
        {
            var parentScope = CreateParentScope(previous);
            var binder = new Binder(parentScope, null);
            foreach (var function in compilationUnit.Members.OfType<FunctionDeclarationSyntax>())
                binder.BindFunctionDeclaration(function);

            var statements = ImmutableArray.CreateBuilder<BoundStatement>();

            foreach (var globalStatement in compilationUnit.Members.OfType<GlobalStatementSyntax>())
            {
                var s = binder.BindStatement(globalStatement.Statement);
                statements.Add(s);
            }

            var functions = binder._scope.GetDeclaredFunctions();
            var variables = binder._scope.GetDeclaredVariables();
            var diagnostics = binder.Diagnostics.ToImmutableArray();

            if (previous != null)
            {
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
            }

            return new BoundGlobalScope(previous, diagnostics, functions, variables, statements.ToImmutable());
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            var parentScope = CreateParentScope(globalScope);

            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            var scope = globalScope;
            while (scope != null)
            {
                foreach (var function in scope.Functions)
                {
                    var binder = new Binder(parentScope, function);
                    var body = binder.BindStatement(function.Declaration.Body);
                    var loweredBody = Lowerer.Lower(body);
                    functionBodies.Add(function, loweredBody);

                    diagnostics.AddRange(binder.Diagnostics);
                }

                scope = scope.Previous;
            }
            var statement = Lowerer.Lower(new BoundBlockStatement(globalScope.Statements));
            return new BoundProgram(globalScope, diagnostics.ToImmutable(), functionBodies.ToImmutable(), statement);
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
                    Diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Span, parameterName);
                }
                else
                {
                    var parameter = new ParameterSymbol(parameterName, parameterType);
                    parameters.Add(parameter);
                }
            }

            var type = BindTypeClause(syntax.Type) ?? TypeSymbol.Void;

            if (type != TypeSymbol.Void)
                Diagnostics.XXX_ReportFunctionsAreUnsupported(syntax.Type.Span);

            var function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax);
            if (!_scope.TryDeclareFunction(function))
                Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Span, function.Name);
        }

        private BoundStatement BindStatement(StatementSyntax statementSyntax)
        {
            switch (statementSyntax.Kind)
            {
                case SyntaxKind.BlockStatement:
                    return BindBlockStatement((BlockStatementSyntax) statementSyntax);
                case SyntaxKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax)statementSyntax);
                case SyntaxKind.VariableDeclarationStatement:
                    return BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)statementSyntax);
                case SyntaxKind.IfStatement:
                    return BindIfStatement((IfStatementSyntax)statementSyntax);
                case SyntaxKind.WhileStatement:
                    return BindWhileStatement((WhileStatementSyntax)statementSyntax);
                case SyntaxKind.ForStatement:
                    return BindForStatement((ForStatementSyntax)statementSyntax);
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

            _scope = _scope.Parent;
            return new BoundBlockStatement(statements.ToImmutable());
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var boundExpression = BindExpression(syntax.Expression, true);
            return new BoundExpressionStatement(boundExpression);
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            var isReadOnly = syntax.Keyword.Kind == SyntaxKind.ConstKeyword;
            var type = BindTypeClause(syntax.TypeClause);
            var initializer = BindExpression(syntax.Initializer);
            var variableType = type ?? initializer.Type;
            var variable = BindVariableDeclaration(syntax.Identifier, isReadOnly, variableType);
            var convertedInitializer = BindConversion(syntax.Initializer.Span, initializer, variableType, false);

            return new BoundVariableDeclarationStatement(variable, convertedInitializer);
        }

        private TypeSymbol BindTypeClause(TypeClauseSyntax syntax)
        {
            if (syntax == null)
                return null;

            var type = TypeSymbol.LookupType(syntax.Identifier.Text);
            if (type == null)
                Diagnostics.ReportUndefinedType(syntax.Identifier.Span, syntax.Identifier.Text);

            return type;
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isReadOnly, TypeSymbol type)
        {
            var name = identifier.Text ?? "?";
            var declare = !identifier.IsMissing;
            var variable = _function == null
                ? (VariableSymbol)new GlobalVariableSymbol(name, isReadOnly, type)
                : new LocalVariableSymbol(name, isReadOnly, type);

            if (declare && !_scope.TryDeclareVariable(variable))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(identifier.Span, name);
            }

            return variable;
        }

        private VariableSymbol BindVariableReference(string name, TextSpan span)
        {
            var symbol = _scope.TryLookupSymbol(name);
            if (symbol == null)
            {
                Diagnostics.ReportUndefinedVariable(span, name);
                return null;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.LocalVariable:
                case SymbolKind.GlobalVariable:
                case SymbolKind.Parameter:
                    return (VariableSymbol)symbol;

                default:
                    Diagnostics.ReportNotAVariable(span, name);
                    return null;
            }
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var thenStatement = BindStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause == null ? null : BindStatement(syntax.ElseClause.ElseStatement);
            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var thenStatement = BindStatement(syntax.Body);
            return new BoundWhileStatement(condition, thenStatement);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            _scope = new BoundScope(_scope);

            var variable = BindVariableDeclaration(syntax.Identifier, false, TypeSymbol.Int);

            var lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int);
            var upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int);

            var step = syntax.StepClause != null ? 
                BindExpression(syntax.StepClause.Expression, TypeSymbol.Int) : 
                new BoundLiteralExpression(1);

            var body = BindStatement(syntax.Body);

            _scope = _scope.Parent;

            return new BoundForStatement(variable, lowerBound, upperBound, step, body);
        }

        private static BoundScope CreateParentScope(BoundGlobalScope previous)
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
                result.TryDeclareFunction(f);

            return result;
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, bool canBeVoid = false)
        {
            var result = BindExpressionInternal(syntax);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                Diagnostics.ReportExpressionMustHaveValue(syntax.Span);
                return new BoundErrorExpression();
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
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);

            if (boundOperand.Kind == BoundNodeKind.ErrorExpression)
            {
                return new BoundErrorExpression();
            }

            var boundOperatorKind = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);
            if (boundOperatorKind == null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
                return new BoundErrorExpression();
            }
            return new BoundUnaryExpression(boundOperand, boundOperatorKind);
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
                return new BoundErrorExpression();
            }

            var boundOperatorKind = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (boundOperatorKind == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression();
            }
            return new BoundBinaryExpression(boundLeft, boundOperatorKind, boundRight);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax)
        {
            if (syntax.Arguments.Count == 1 && TypeSymbol.LookupType(syntax.Identifier.Text) is TypeSymbol type)
            {
                return BindConversion(syntax.Arguments[0], type, true);
            }

            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (var argument in syntax.Arguments)
            {
                var boundArgument = BindExpression(argument);
                boundArguments.Add(boundArgument);
            }
            var symbol = _scope.TryLookupSymbol(syntax.Identifier.Text);
            if (symbol == null)
            {
                Diagnostics.ReportUndefinedFunction(syntax.Identifier.Span, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            var function = symbol as FunctionSymbol;
            if (function == null)
            {
                Diagnostics.ReportNotAFunction(syntax.Identifier.Span, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            if (syntax.Arguments.Count != function.Parameters.Length)
            {
                Diagnostics.ReportWrongArgumentCount(syntax.Span, function.Name, function.Parameters.Length, syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            for (var i = 0; i < syntax.Arguments.Count; i++)
            {
                var argument = boundArguments[i];
                var parameter = function.Parameters[i];

                if (argument.Type != parameter.Type)
                {
                    Diagnostics.ReportWrongArgumentType(syntax.Arguments[i].Span, parameter.Name, parameter.Type, argument.Type);
                    return new BoundErrorExpression();
                }
            }

            return new BoundCallExpression(function, boundArguments.ToImmutable());
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit)
        {
            var expression = BindExpression(syntax);
            return BindConversion(syntax.Span, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextSpan diagnosticSpan, BoundExpression expression, TypeSymbol type, bool allowExplicit)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
                {
                    Diagnostics.ReportCannotConvert(diagnosticSpan, expression.Type, type);
                }
                return new BoundErrorExpression();
            }

            if (!allowExplicit && conversion.IsExplicit)
            {
                Diagnostics.ReportCannotConvertImplicitly(diagnosticSpan, expression.Type, type);
            }

            if (conversion.IsIdentity)
            {
                return expression;
            }

            return new BoundConversionExpression(type, expression);
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            if (syntax.IdentifierToken.IsMissing)
            {
                // This means the token was inserted by the parser.
                // An error has already been reported so we can just return an error expression.
                return new BoundErrorExpression();
            }
                
            var name = syntax.IdentifierToken.Text;
            var variable = BindVariableReference(name, syntax.IdentifierToken.Span);
            if (variable == null)
            {
                return new BoundErrorExpression();
            }
            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            var variable = BindVariableReference(name, syntax.IdentifierToken.Span);
            if(variable == null)
            {
                return boundExpression;
            }

            if (variable.IsReadOnly)
            {
                Diagnostics.ReportCannotReassigned(syntax.EqualsToken.Span, name);
            }

            var convertedExpression = BindConversion(syntax.Expression.Span, boundExpression, variable.Type, false);
            return new BoundAssignmentExpression(variable, convertedExpression);
        }
    }
}
