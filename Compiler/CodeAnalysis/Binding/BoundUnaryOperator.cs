using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryOperator
    {
        public SyntaxKind SyntaxKind { get; }
        public BoundUnaryOperatorKind Kind { get; }
        public TypeSymbol OperandType { get; }
        public TypeSymbol ResultType { get; }

        private BoundUnaryOperator(SyntaxKind syntaxKind,
            BoundUnaryOperatorKind kind,
            TypeSymbol operandType) :
            this(syntaxKind, kind, operandType, operandType)
        {
        }

        private BoundUnaryOperator(SyntaxKind syntaxKind,
            BoundUnaryOperatorKind kind,
            TypeSymbol operandType,
            TypeSymbol resultType)
        {
            SyntaxKind = syntaxKind;
            Kind = kind;
            OperandType = operandType;
            ResultType = resultType;
        }

        private static readonly BoundUnaryOperator[] Operators =
        {
            new BoundUnaryOperator(SyntaxKind.BangToken, BoundUnaryOperatorKind.LogicalNegation, TypeSymbol.Bool),
            new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.Int),
            new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.Int),
            new BoundUnaryOperator(SyntaxKind.TildeToken, BoundUnaryOperatorKind.OnesComplement, TypeSymbol.Int),
        };

        public static BoundUnaryOperator Bind(SyntaxKind syntaxKind, TypeSymbol operandType)
        {
            foreach (var boundUnaryOperator in Operators)
            {
                if (boundUnaryOperator.SyntaxKind == syntaxKind && boundUnaryOperator.OperandType == operandType)
                {
                    return boundUnaryOperator;
                }
            }
            return null;
        }
    }
}