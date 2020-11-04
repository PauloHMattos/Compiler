namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.GlobalVariable;

        internal GlobalVariableSymbol(string name, bool isReadOnly, TypeSymbol type)
            : base(name, isReadOnly, type)
        {
        }
    }
}