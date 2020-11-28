using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundExpression Operand { get; }
        public BoundUnaryOperator Operator { get; }
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override TypeSymbol Type => Operator.ResultType;
        public override BoundConstant? ConstantValue { get; }

        public BoundUnaryExpression(SyntaxNode syntax, BoundExpression operand, BoundUnaryOperator op)
            : base(syntax)
        {
            Operand = operand;
            Operator = op;
            ConstantValue = ConstantFolding.Fold(op, operand);
        }
    }
}