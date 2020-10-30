using System;
using System.Collections.Generic;
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
            var boundOperatorKind = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);
            if (boundOperatorKind == null)
            {
                _diagnostics.Add($"Unary operator '{syntax.OperatorToken.Kind}' is not defined for type '{boundOperand.Type}'");
                return boundOperand;
            }
            return new BoundUnaryExpression(boundOperand, boundOperatorKind);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);
            var boundOperatorKind = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (boundOperatorKind == null)
            {
                _diagnostics.Add($"Binary operator '{syntax.OperatorToken.Kind}' is not defined for types '{boundLeft.Type}' and '{boundRight.Type}'");
                return boundLeft;
            }
            return new BoundBinaryExpression(boundLeft, boundOperatorKind, boundRight);
        }
    }
}
