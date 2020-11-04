namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundDoWhileStatement : BoundLoopStatement
    {
        public BoundExpression Condition { get; }
        public BoundStatement Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStatement;

        public BoundDoWhileStatement(BoundExpression condition, BoundStatement body,
            BoundLabel breakLabel,
            BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Condition = condition;
            Body = body;
        }
    }
}