namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundLabelStatement : BoundStatement
    {
        public BoundLabel Label { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;

        public BoundLabelStatement(BoundLabel label)
        {
            Label = label;
        }
    }
}