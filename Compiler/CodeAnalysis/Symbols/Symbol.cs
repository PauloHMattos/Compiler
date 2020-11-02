namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class Symbol
    {
        public string Name { get; }
        public abstract SymbolKind Kind { get; }

        internal Symbol(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}