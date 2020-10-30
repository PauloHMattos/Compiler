using System;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundExpression Operand { get; }
        public BoundUnaryOperatorKind OperatorKind { get; }
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override Type Type => Operand.Type;

        public BoundUnaryExpression(BoundExpression operand, BoundUnaryOperatorKind kind)
        {
            Operand = operand;
            OperatorKind = kind;
        }
    }
}