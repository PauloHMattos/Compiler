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

                default:
                    throw new Exception($"Unexpected node {root.Kind}");
            }
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression unaryExpression)
        {
            var operand = EvaluateExpression(unaryExpression.Operand);

            switch (unaryExpression.OperatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return (int)operand;
                case BoundUnaryOperatorKind.Negation:
                    return -(int)operand;
                case BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool)operand;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression binaryExpression)
        {
            var left = EvaluateExpression(binaryExpression.Left);
            var right = EvaluateExpression(binaryExpression.Right);

            switch (binaryExpression.OperatorKind)
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


                //case BoundBinaryOperatorKind.BitwiseAnd:
                //    if (left is bool bLeft)
                //    {
                //        return bLeft & (bool)right;
                //    }
                //    return (int)left & (int)right;
                //case BoundBinaryOperatorKind.BitwiseOr:
                //    return (bool)left || (bool)right;

                default:
                    throw new Exception($"Unexpected binary operator {binaryExpression.OperatorKind}");
            }
        }
    }
}