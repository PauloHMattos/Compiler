using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class ParameterSymbol : VariableSymbol
    {
        public int Ordinal { get; }
        public override SymbolKind Kind => SymbolKind.Parameter;

        internal ParameterSymbol(string name, TypeSymbol type, int ordinal)
            : base(name, true, type, null)
        {
            Ordinal = ordinal;
        }
    }
}