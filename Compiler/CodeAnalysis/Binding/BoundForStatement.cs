using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundForStatement : BoundStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression LowerBound { get; }
        public BoundExpression UpperBound { get; }
        public BoundExpression Step { get; }
        public BoundStatement Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

        public BoundForStatement(VariableSymbol variable, 
            BoundExpression lowerBound,
            BoundExpression upperBound, 
            BoundExpression step,
            BoundStatement body)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Step = step;
            Body = body;
        }
    }
}