using System;
using Compiler.CodeAnalysis;

namespace Compiler.Evaluation
{
    public class Evaluator
    {
        private readonly ExpressionSyntax _root;

        public Evaluator(ExpressionSyntax root)
        {
            _root = root;
        }

        public int Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private int EvaluateExpression(ExpressionSyntax root)
        {
            switch (root.Kind)
            {
                case SyntaxKind.LiteralExpression:
                    var numberExpression = (LiteralExpressionSyntax)root;
                    return (int)numberExpression.LiteralToken.Value;

                case SyntaxKind.UnaryExpression:
                    var unaryExpression = (UnaryExpressionSyntax)root;
                    return EvaluateUnaryExpression(unaryExpression);

                case SyntaxKind.BinaryExpression:
                    var binaryExpression = (BinaryExpressionSyntax)root;
                    return EvaluateBinaryExpression(binaryExpression);

                case SyntaxKind.ParenthesizedExpression:
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)root;
                    return EvaluateExpression(parenthesizedExpression.Expression);

                default:
                    throw new Exception($"Unexpected node {root.Kind}");
            }
        }

        private int EvaluateUnaryExpression(UnaryExpressionSyntax unaryExpression)
        {
            switch (unaryExpression.OperatorToken.Kind)
            {
                case SyntaxKind.PlusToken:
                    return EvaluateExpression(unaryExpression.Operand);

                case SyntaxKind.MinusToken:
                    return -EvaluateExpression(unaryExpression.Operand);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int EvaluateBinaryExpression(BinaryExpressionSyntax binaryExpression)
        {
            var left = EvaluateExpression(binaryExpression.Left);
            var right = EvaluateExpression(binaryExpression.Right);

            switch (binaryExpression.OperatorToken.Kind)
            {
                case SyntaxKind.PlusToken:
                    return left + right;
                case SyntaxKind.MinusToken:
                    return left - right;
                case SyntaxKind.StarToken:
                    return left * right;
                case SyntaxKind.SlashToken:
                    return left / right;
                default:
                    throw new Exception($"Unexpected binary operator {binaryExpression.OperatorToken.Kind}");
            }
        }
    }
}