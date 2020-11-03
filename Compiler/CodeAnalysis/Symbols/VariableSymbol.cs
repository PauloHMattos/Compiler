namespace Compiler.CodeAnalysis.Symbols
{
    public class VariableSymbol : Symbol
    {
        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }
        public override SymbolKind Kind => SymbolKind.Variable;

        internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type) : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }
    }
}