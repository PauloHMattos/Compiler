using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : TypedSymbol
    {
        public bool IsReadOnly { get; }

        private protected VariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constant)
            : base(name, type, isReadOnly ? constant : null)
        {
            IsReadOnly = isReadOnly;
        }
    }
}