namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundConditionalGotoStatement : BoundStatement
    {
        public BoundLabel Label { get; }
        public BoundExpression Condition { get; }
        public bool JumpIfTrue { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

        public BoundConditionalGotoStatement(BoundLabel label, BoundExpression condition, bool jumpIfTrue)
        {
            Label = label;
            Condition = condition;
            JumpIfTrue = jumpIfTrue;
        }
    }
}