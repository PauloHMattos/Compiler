using System;
using System.Collections.Generic;
using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundStatement _root;
        private readonly Dictionary<VariableSymbol, object> _variables;
        private object _lastValue;

        public Evaluator(BoundStatement root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        public object Evaluate()
        {
            EvaluateStatement(_root);
            return _lastValue;
        }

        private void EvaluateStatement(BoundStatement statement)
        {
            switch (statement.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    EvaluateBlockStatement((BoundBlockStatement)statement);
                    break;

                case BoundNodeKind.ExpressionStatement:
                    EvaluateExpressionStatement((BoundExpressionStatement)statement);
                    break;

                case BoundNodeKind.VariableDeclarationStatement:
                    EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)statement);
                    break;

                case BoundNodeKind.IfStatement:
                    EvaluateIfStatement((BoundIfStatement)statement);
                    break;

                case BoundNodeKind.WhileStatement:
                    EvaluateWhileStatement((BoundWhileStatement)statement);
                    break;

                case BoundNodeKind.ForStatement:
                    EvaluateForStatement((BoundForStatement)statement);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected expression {statement.Kind}");
            }
        }

        private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement statement)
        {
            var value = EvaluateExpression(statement.Initializer);
            _variables[statement.Variable] = value;
            _lastValue = value;
        }

        private void EvaluateIfStatement(BoundIfStatement statement)
        {
            var condition = (bool)EvaluateExpression(statement.Condition);
            if (condition)
            {
                EvaluateStatement(statement.ThenStatement);
            }
            else if (statement.ElseStatement != null)
            {
                EvaluateStatement(statement.ElseStatement);
            }
        }

        private void EvaluateWhileStatement(BoundWhileStatement statement)
        {
            while ((bool)EvaluateExpression(statement.Condition))
            {
                EvaluateStatement(statement.Body);
            }
        }

        private void EvaluateForStatement(BoundForStatement statement)
        {
            var lowerBound = (int)EvaluateExpression(statement.LowerBound);
            var upperBound = (int)EvaluateExpression(statement.UpperBound);
            var step = (int)EvaluateExpression(statement.Step);

            if (step < 0)
            {
                step = -step;
                var aux = lowerBound;
                lowerBound = upperBound;
                upperBound = aux;
            }

            _variables[statement.Variable] = lowerBound;
            for (var i = lowerBound; i <= upperBound; i += step)
            {
                _variables[statement.Variable] = i;
                EvaluateStatement(statement.Body);
            }
        }

        private void EvaluateBlockStatement(BoundBlockStatement blockStatement)
        {
            foreach (var statement in blockStatement.Statements)
            {
                EvaluateStatement(statement);
            }
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement expressionStatement)
        {
            _lastValue = EvaluateExpression(expressionStatement.Expression);
        }

        private object EvaluateExpression(BoundExpression expression)
        {
            switch (expression.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    var boundLiteralExpression = (BoundLiteralExpression)expression;
                    return boundLiteralExpression.Value;

                case BoundNodeKind.VariableExpression:
                    var boundVariableExpression = (BoundVariableExpression)expression;
                    return _variables[boundVariableExpression.VariableSymbol];

                case BoundNodeKind.AssignmentExpression:
                    var assignmentExpression = (BoundAssignmentExpression)expression;
                    var value = EvaluateExpression(assignmentExpression.Expression);
                    _variables[assignmentExpression.VariableSymbol] = value;
                    return value;

                case BoundNodeKind.UnaryExpression:
                    var boundUnaryExpression = (BoundUnaryExpression)expression;
                    return EvaluateUnaryExpression(boundUnaryExpression);

                case BoundNodeKind.BinaryExpression:
                    var boundBinaryExpression = (BoundBinaryExpression)expression;
                    return EvaluateBinaryExpression(boundBinaryExpression);

                default:
                    throw new InvalidOperationException($"Unexpected expression {expression.Kind}");
            }
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression unaryExpression)
        {
            var operand = EvaluateExpression(unaryExpression.Operand);

            switch (unaryExpression.Operator.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return (int)operand;
                case BoundUnaryOperatorKind.Negation:
                    return -(int)operand;
                case BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool)operand;
                case BoundUnaryOperatorKind.OnesComplement:
                    return ~(int)operand;
                default:
                    throw new InvalidOperationException($"Unexpected unary operator {unaryExpression.Operator.Kind}");
            }
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression binaryExpression)
        {
            var left = EvaluateExpression(binaryExpression.Left);
            var right = EvaluateExpression(binaryExpression.Right);

            switch (binaryExpression.Operator.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    return (int)left + (int)right;
                case BoundBinaryOperatorKind.Subtraction:
                    return (int)left - (int)right;
                case BoundBinaryOperatorKind.Multiplication:
                    return (int)left * (int)right;
                case BoundBinaryOperatorKind.Division:
                    return (int)left / (int)right;
                case BoundBinaryOperatorKind.LogicalAnd:
                    return (bool)left && (bool)right;
                case BoundBinaryOperatorKind.LogicalOr:
                    return (bool)left || (bool)right;
                case BoundBinaryOperatorKind.Equals:
                    return left.Equals(right);
                case BoundBinaryOperatorKind.NotEquals:
                    return !left.Equals(right);
                case BoundBinaryOperatorKind.Less:
                    return (int)left < (int)right;
                case BoundBinaryOperatorKind.LessOrEquals:
                    return (int)left <= (int)right;
                case BoundBinaryOperatorKind.Greater:
                    return (int)left > (int)right;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return (int)left >= (int)right;
                case BoundBinaryOperatorKind.BitwiseAnd:
                    if (binaryExpression.Type == (typeof(int)))
                        return (int)left & (int)right;
                    else
                        return (bool)left & (bool)right;
                case BoundBinaryOperatorKind.BitwiseOr:
                    if (binaryExpression.Type == (typeof(int)))
                        return (int)left | (int)right;
                    else
                        return (bool)left | (bool)right;
                case BoundBinaryOperatorKind.BitwiseXor:
                    if (binaryExpression.Type == (typeof(int)))
                        return (int)left ^ (int)right;
                    else
                        return (bool)left ^ (bool)right;
                default:
                    throw new InvalidOperationException($"Unexpected binary operator {binaryExpression.Operator.Kind}");
            }
        }
    }
}