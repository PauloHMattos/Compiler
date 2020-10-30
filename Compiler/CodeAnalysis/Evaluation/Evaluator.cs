using System;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Evaluation
{
    public class Evaluator
    {
        private readonly BoundExpression _root;

        public Evaluator(BoundExpression root)
        {
            _root = root;
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

                case BoundNodeKind.UnaryExpression:
                    var boundUnaryExpression = (BoundUnaryExpression)root;
                    return EvaluateUnaryExpression(boundUnaryExpression);

                case BoundNodeKind.BinaryExpression:
                    var boundBinaryExpression = (BoundBinaryExpression)root;
                    return EvaluateBinaryExpression(boundBinaryExpression);

                //case BoundNodeKind.ParenthesizedExpression:
                //    var parenthesizedExpression = (ParenthesizedExpressionSyntax)root;
                //    return EvaluateExpression(parenthesizedExpression.Expression);

                default:
                    throw new Exception($"Unexpected node {root.Kind}");
            }
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression unaryExpression)
        {
            var operand = (int)EvaluateExpression(unaryExpression.Operand);
            
            switch (unaryExpression.OperatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return operand;

                case BoundUnaryOperatorKind.Negation:
                    return -operand;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression binaryExpression)
        {
            var left = (int)EvaluateExpression(binaryExpression.Left);
            var right = (int)EvaluateExpression(binaryExpression.Right);

            switch (binaryExpression.OperatorKind)
            {
                case BoundBinaryOperatorKind.Addition:
                    return left + right;
                case BoundBinaryOperatorKind.Subtraction:
                    return left - right;
                case BoundBinaryOperatorKind.Multiplication:
                    return left * right;
                case BoundBinaryOperatorKind.Division:
                    return left / right;
                default:
                    throw new Exception($"Unexpected binary operator {binaryExpression.OperatorKind}");
            }
        }
    }
}