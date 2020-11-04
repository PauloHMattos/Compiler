namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundDoWhileStatement : BoundStatement
    {
        public BoundExpression Condition { get; }
        public BoundStatement Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStatement;

        public BoundDoWhileStatement(BoundExpression condition, BoundStatement body)
        {
            Condition = condition;
            Body = body;
        }
    }
}