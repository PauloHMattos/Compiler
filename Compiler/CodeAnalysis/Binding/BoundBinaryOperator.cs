using System;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryOperator
    {
        public SyntaxKind SyntaxKind { get; }
        public BoundBinaryOperatorKind Kind { get; }
        public Type LeftType { get; }
        public Type RightType { get; }
        public Type ResultType { get; }

        private BoundBinaryOperator(SyntaxKind syntaxKind,
            BoundBinaryOperatorKind kind,
            Type type) :
            this(syntaxKind, kind, type, type, type)
        {
        }

        private BoundBinaryOperator(SyntaxKind syntaxKind,
            BoundBinaryOperatorKind kind,
            Type operandType, Type resultType) :
            this(syntaxKind, kind, operandType, operandType, resultType)
        {
        }

        private BoundBinaryOperator(SyntaxKind syntaxKind,
            BoundBinaryOperatorKind kind,
            Type leftType,
            Type rightType,
            Type resultType)
        {
            SyntaxKind = syntaxKind;
            Kind = kind;
            LeftType = leftType;
            RightType = rightType;
            ResultType = resultType;
        }

        private static BoundBinaryOperator[] _operators =
        {
            new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.Subtraction, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.Multiplication, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.Division, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.EqualsEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Int, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.BangEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Int, TypeSymbol.Bool),
            
            new BoundBinaryOperator(SyntaxKind.AmpersandAmpersandToken, BoundBinaryOperatorKind.LogicalAnd, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.PipePipeToken, BoundBinaryOperatorKind.LogicalOr, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.EqualsEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.BangEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Bool),
        };

        public static BoundBinaryOperator Bind(SyntaxKind syntaxKind, Type leftType, Type rightType)
        {
            foreach (var boundBinaryOperator in _operators)
            {
                if (boundBinaryOperator.SyntaxKind == syntaxKind &&
                    boundBinaryOperator.LeftType == leftType &&
                    boundBinaryOperator.RightType == rightType)
                {
                    return boundBinaryOperator;
                }
            }
            return null;
        }
    }
}