using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundCompoundAssignmentExpression : BoundExpression
    {
	    public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentExpression;
        public override TypeSymbol Type => Expression.Type;
        public VariableSymbol Variable { get; }
        public BoundBinaryOperator Operator {get; }
        public BoundExpression Expression { get; }

        public BoundCompoundAssignmentExpression(VariableSymbol variable, BoundBinaryOperator op, BoundExpression expression)
        {
            Variable = variable;
            Operator = op;
            Expression = expression;
        }
    }
}