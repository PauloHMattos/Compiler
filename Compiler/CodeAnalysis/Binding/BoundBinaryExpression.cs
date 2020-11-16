using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundExpression Left { get; }
        public BoundBinaryOperator Operator { get; }
        public BoundExpression Right { get; }
        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override TypeSymbol Type => Operator.ResultType;
        public override BoundConstant? ConstantValue { get; }

        public BoundBinaryExpression(SyntaxNode syntax,
                                     BoundExpression left,
                                     BoundBinaryOperator op,
                                     BoundExpression right)
            : base(syntax)
        {
            Left = left;
            Operator = op;
            Right = right;
            ConstantValue = ConstantFolding.Fold(left, op, right);
        }
    }
}