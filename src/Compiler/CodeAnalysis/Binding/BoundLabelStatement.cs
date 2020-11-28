using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundLabelStatement : BoundStatement
    {
        public BoundLabel Label { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;

        public BoundLabelStatement(SyntaxNode syntax, BoundLabel label)
            : base(syntax)
        {
            Label = label;
        }
    }
}