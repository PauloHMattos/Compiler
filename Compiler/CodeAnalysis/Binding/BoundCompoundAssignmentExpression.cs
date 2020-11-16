using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundCompoundAssignmentExpression : BoundExpression
    {
	    public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentExpression;
        public override TypeSymbol Type => Expression.Type;
        public VariableSymbol Variable { get; }
        public BoundBinaryOperator Operator {get; }
        public BoundExpression Expression { get; }

        public BoundCompoundAssignmentExpression(SyntaxNode syntax,
                                                 VariableSymbol variable,
                                                 BoundBinaryOperator op,
                                                 BoundExpression expression)
            : base(syntax)
        {
            Variable = variable;
            Operator = op;
            Expression = expression;
        }
    }
}