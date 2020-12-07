using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class TypedSymbol : Symbol
    {
        public TypeSymbol Type { get; }
        internal virtual BoundConstant? Constant { get; }

        private protected TypedSymbol(string name, TypeSymbol type, BoundConstant? constant) : base(name)
        {
            Type = type;
            Constant = constant;
        }
    }
}