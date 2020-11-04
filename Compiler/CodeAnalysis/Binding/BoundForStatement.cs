using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundForStatement : BoundLoopStatement
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
            BoundStatement body, 
            BoundLabel breakLabel, 
            BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Step = step;
            Body = body;
        }
    }

    internal abstract class BoundLoopStatement : BoundStatement
    {
        protected BoundLoopStatement(BoundLabel breakLabel, BoundLabel continueLabel)
        {
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
        }

        public BoundLabel BreakLabel { get; }
        public BoundLabel ContinueLabel { get; }
    }
}