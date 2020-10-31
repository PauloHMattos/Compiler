﻿using System.Collections.Generic;
using System.Data;
using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundExpression _root;
        private readonly Dictionary<VariableSymbol, object> _variables;

        public Evaluator(BoundExpression root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        public object Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private object EvaluateExpression(BoundExpression root)
        {
            switch (root.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    var boundLiteralExpression = (BoundLiteralExpression)root;
                    return boundLiteralExpression.Value;

                case BoundNodeKind.VariableExpression:
                    var boundVariableExpression = (BoundVariableExpression)root;
                    return _variables[boundVariableExpression.VariableSymbol];

                case BoundNodeKind.AssignmentExpression:
                    var assignmentExpression = (BoundAssignmentExpression)root;
                    var value = EvaluateExpression(assignmentExpression.Expression);
                    _variables[assignmentExpression.VariableSymbol] = value;
                    return value;

                case BoundNodeKind.UnaryExpression:
                    var boundUnaryExpression = (BoundUnaryExpression)root;
                    return EvaluateUnaryExpression(boundUnaryExpression);

                case BoundNodeKind.BinaryExpression:
                    var boundBinaryExpression = (BoundBinaryExpression)root;
                    return EvaluateBinaryExpression(boundBinaryExpression);

                default:
                    throw new InvalidExpressionException($"Unexpected node {root.Kind}");
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

                default:
                    throw new InvalidExpressionException($"Unexpected unary operator {unaryExpression.Operator.Kind}");
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

                default:
                    throw new InvalidExpressionException($"Unexpected binary operator {binaryExpression.Operator.Kind}");
            }
        }
    }
}