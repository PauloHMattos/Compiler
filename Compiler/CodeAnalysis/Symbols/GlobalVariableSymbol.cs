using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.GlobalVariable;

        internal GlobalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant constant)
            : base(name, isReadOnly, type, constant)
        {
        }
    }
}