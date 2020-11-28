using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundGotoStatement : BoundStatement
    {
        public BoundLabel Label { get; }
        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

        public BoundGotoStatement(SyntaxNode syntax, BoundLabel label)
            : base(syntax)
        {
            Label = label;
        }
    }
}