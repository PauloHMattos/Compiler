using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }
        internal virtual BoundConstant? Constant { get; }

        internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constant) : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
            Constant = isReadOnly ? constant : null;
        }
    }
}