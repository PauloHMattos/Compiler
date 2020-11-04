namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }

        protected VariableSymbol(string name, bool isReadOnly, TypeSymbol type) : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }
    }
}