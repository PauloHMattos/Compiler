namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundGotoStatement : BoundStatement
    {
        public BoundLabel Label { get; }
        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

        public BoundGotoStatement(BoundLabel label)
        {
            Label = label;
        }
    }
}