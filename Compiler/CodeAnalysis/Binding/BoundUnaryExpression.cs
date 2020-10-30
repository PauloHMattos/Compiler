using System;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundExpression Operand { get; }
        public BoundUnaryOperator Operator { get; }
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override Type Type => Operator.ResultType;

        public BoundUnaryExpression(BoundExpression operand, BoundUnaryOperator op)
        {
            Operand = operand;
            Operator = op;
        }
    }
}