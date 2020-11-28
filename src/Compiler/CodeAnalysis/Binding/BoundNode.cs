using System.IO;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    public abstract class BoundNode
    {
        public SyntaxNode Syntax { get; }
        public abstract BoundNodeKind Kind { get; }

        private protected BoundNode(SyntaxNode syntax)
        {
            Syntax = syntax;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteTo(writer);
            return writer.ToString();
        }
    }
}