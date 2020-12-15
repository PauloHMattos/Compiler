using System.IO;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class Symbol
    {
        public SyntaxNode? Syntax { get; }
        public string Name { get; }
        public abstract SymbolKind Kind { get; }

        private protected Symbol(SyntaxNode? syntax, string name)
        {
            Syntax = syntax;
            Name = name;
        }

        public void WriteTo(TextWriter writer)
        {
            SymbolPrinter.WriteTo(this, writer);
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }
    }
}