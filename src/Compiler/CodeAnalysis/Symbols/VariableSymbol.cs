using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class VariableSymbol : TypedSymbol
    {
        public bool IsReadOnly { get; }
        public bool IsStatic { get; }
        public VariableKind VariableKind { get; }
        public override SymbolKind Kind => SymbolKind.Variable;

        private VariableSymbol(string name,
                                bool isReadOnly,
                                bool isStatic,
                                VariableKind variableKind,
                                TypeSymbol type,
                                BoundConstant? constant)
            : base(name, type, isReadOnly ? constant : null)
        {
            IsReadOnly = isReadOnly;
            IsStatic = isStatic;
            VariableKind = variableKind;
        }

        internal static VariableSymbol New(string name,
                                           bool isReadOnly,
                                           bool isStatic,
                                           VariableKind variableKind,
                                           TypeSymbol type,
                                           BoundConstant? constant = null)
        {
            return new VariableSymbol(name, isReadOnly, isStatic, variableKind, type, constant);
        }

        internal static VariableSymbol Local(string name,
                                             bool isReadOnly,
                                             TypeSymbol type,
                                             BoundConstant? constant = null)
        {
            return new VariableSymbol(name, isReadOnly, false, VariableKind.Local, type, constant);
        }

        internal static VariableSymbol Parameter(string name,
                                                 bool isReadOnly,
                                                 TypeSymbol type,
                                                 BoundConstant? constant = null)
        {
            return new VariableSymbol(name, isReadOnly, false, VariableKind.Parameter, type, constant);
        }
    }
}