namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class ParameterSymbol : VariableSymbol
    {
        public int Ordinal { get; }
        public override SymbolKind Kind => SymbolKind.Parameter;

        public ParameterSymbol(string name, TypeSymbol type, int ordinal)
            : base(name, true, type)
        {
            Ordinal = ordinal;
        }
    }
}