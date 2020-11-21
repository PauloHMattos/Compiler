using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class EnumValueSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.Parameter;
        internal override BoundConstant Constant => base.Constant!;

        internal EnumValueSymbol(string name, int value)
            : base(name, true, TypeSymbol.Int, new BoundConstant (value))
        {
        }
    }
}