using System;
using System.Collections.Generic;
using System.Threading;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    public sealed class Binder
    {
        private readonly List<string> _diagnostics;
        
        private Binder()
        {
            _diagnostics = new List<string>();
        }

        public static BoundExpression Bind(ExpressionSyntax syntax, out IEnumerable<string> diagnostics)
        {
            var binder = new Binder();
            var boundExpression = binder.BindExpression(syntax);
            diagnostics = binder._diagnostics;
            return boundExpression;
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax)
        {
            var kind = syntax.Kind;
            switch (kind)
            {
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax)syntax);
                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)syntax);
                case SyntaxKind.ParenthesizedExpression:
                    return BindExpression(((ParenthesizedExpressionSyntax)syntax).Expression);
                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)syntax);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, $"Unexpected syntax {kind}");
            }
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);
            var boundOperatorKind = BindUnaryOperatorKind(syntax.OperatorToken.Kind, boundOperand.Type);
            if (!boundOperatorKind.HasValue)
            {
                _diagnostics.Add($"Unary operator '{syntax.OperatorToken.Kind}' is not defined for type '{boundOperand.Type}'");
                return boundOperand;
            }
            return new BoundUnaryExpression(boundOperand, boundOperatorKind.Value);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);
            var boundOperatorKind = BindBinaryOperatorKind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (!boundOperatorKind.HasValue)
            {
                _diagnostics.Add($"Binary operator '{syntax.OperatorToken.Kind}' is not defined for types '{boundLeft.Type}' and '{boundRight.Type}'");
                return boundLeft;
            }
            return new BoundBinaryExpression(boundLeft, boundOperatorKind.Value, boundRight);
        }

        private static BoundUnaryOperatorKind? BindUnaryOperatorKind(SyntaxKind operatorTokenKind, Type operandType)
        {
            if (operandType == typeof(int))
            {
                switch (operatorTokenKind)
                {
                    case SyntaxKind.PlusToken:
                        return BoundUnaryOperatorKind.Identity;
                    case SyntaxKind.MinusToken:
                        return BoundUnaryOperatorKind.Negation;
                }
            }
            else if (operandType == typeof(bool))
            {
                switch (operatorTokenKind)
                {
                    case SyntaxKind.BangToken:
                        return BoundUnaryOperatorKind.LogicalNegation;
                }
            }

            return null;
        }

        private static BoundBinaryOperatorKind? BindBinaryOperatorKind(SyntaxKind operatorTokenKind, Type leftType, Type rightType)
        {
            if (leftType == typeof(int) && leftType == rightType)
            {
                switch (operatorTokenKind)
                {
                    case SyntaxKind.PlusToken:
                        return BoundBinaryOperatorKind.Addition;
                    case SyntaxKind.MinusToken:
                        return BoundBinaryOperatorKind.Subtraction;
                    case SyntaxKind.StarToken:
                        return BoundBinaryOperatorKind.Multiplication;
                    case SyntaxKind.SlashToken:
                        return BoundBinaryOperatorKind.Division;
                }
            }
            else if (leftType == typeof(bool) && leftType == rightType)
            {
                switch (operatorTokenKind)
                {
                    case SyntaxKind.AmpersandAmpersandToken:
                        return BoundBinaryOperatorKind.LogicalAnd;
                    case SyntaxKind.PipePipeToken:
                        return BoundBinaryOperatorKind.LogicalOr;
                }
            }
            return null;
        }
    }
}
