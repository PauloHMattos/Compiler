using System;
using System.Collections.Generic;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundBlockStatement _root;
        private readonly Dictionary<VariableSymbol, object> _variables;
        private object _lastValue;

        public Evaluator(BoundBlockStatement root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        public object Evaluate()
        {
            var labelToIndex = new Dictionary<BoundLabel, int>();
            for (var i = 0; i < _root.Statements.Length; i++)
            {
                var statement = _root.Statements[i];
                if (statement.Kind != BoundNodeKind.LabelStatement)
                {
                    continue;
                }

                var labelStatement = (BoundLabelStatement) statement;
                labelToIndex.Add(labelStatement.Label, i + 1);
            }

            var index = 0;
            while(index < _root.Statements.Length)
            {
                var statement = _root.Statements[index];
                switch (statement.Kind)
                {
                    case BoundNodeKind.ExpressionStatement:
                        EvaluateExpressionStatement((BoundExpressionStatement) statement);
                        index++;
                        break;

                    case BoundNodeKind.VariableDeclarationStatement:
                        EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement) statement);
                        index++;
                        break;

                    case BoundNodeKind.LabelStatement:
                        index++;
                        break;

                    case BoundNodeKind.GotoStatement:
                        var gotoStatement = (BoundGotoStatement) statement;
                        index = labelToIndex[gotoStatement.Label];
                        break;

                    case BoundNodeKind.ConditionalGotoStatement:
                        var conditionalGotoStatement = (BoundConditionalGotoStatement) statement;
                        var condition = (bool)EvaluateExpression(conditionalGotoStatement.Condition);
                        if (condition == conditionalGotoStatement.JumpIfTrue)
                        {
                            index = labelToIndex[conditionalGotoStatement.Label];
                        }
                        else
                        {
                            index++;
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"Unexpected statement {statement.Kind}");
                }
            }
            return _lastValue;
        }

        private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement statement)
        {
            var value = EvaluateExpression(statement.Initializer);
            _variables[statement.Variable] = value;
            _lastValue = value;
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
                    _variables[assignmentExpression.Variable] = value;
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
                    if (binaryExpression.Type == TypeSymbol.Int)
                    {
                        return (int)left & (int)right;
                    }
                    else
                    {
                        return (bool)left & (bool)right;
                    }
                case BoundBinaryOperatorKind.BitwiseOr:
                    if (binaryExpression.Type == TypeSymbol.Int)
                    {
                        return (int)left | (int)right;
                    }
                    else
                    {
                        return (bool)left | (bool)right;
                    }
                case BoundBinaryOperatorKind.BitwiseXor:
                    if (binaryExpression.Type == TypeSymbol.Int)
                    {
                        return (int)left ^ (int)right;
                    }
                    else
                    {
                        return (bool)left ^ (bool)right;
                    }
                default:
                    throw new InvalidOperationException($"Unexpected binary operator {binaryExpression.Operator.Kind}");
            }
        }
    }
}