﻿using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundExpression Operand { get; }
        public BoundUnaryOperator Operator { get; }
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override TypeSymbol Type => Operator.ResultType;
        public override BoundConstant? ConstantValue { get; }

        public BoundUnaryExpression(BoundExpression operand, BoundUnaryOperator op)
        {
            Operand = operand;
            Operator = op;
            ConstantValue = ConstantFolding.Fold(op, operand);
        }
    }
}