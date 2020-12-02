using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundCompoundAssignmentExpression : BoundAssignmentExpression
    {
	    public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentExpression;
        public BoundBinaryOperator Operator {get; }

        public BoundCompoundAssignmentExpression(SyntaxNode syntax,
                                                 BoundExpression left,
                                                 BoundBinaryOperator op,
                                                 BoundExpression right)
            : base(syntax, left, right)
        {
            Operator = op;
        }
    }
}