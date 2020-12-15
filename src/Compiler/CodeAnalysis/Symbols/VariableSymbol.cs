using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class VariableSymbol : TypedSymbol
    {
        public bool IsReadOnly { get; }
        public bool IsStatic { get; }
        public VariableKind VariableKind { get; }
        public override SymbolKind Kind => SymbolKind.Variable;

        private VariableSymbol(SyntaxNode? syntax,
                               string name,
                               bool isReadOnly,
                               bool isStatic,
                               VariableKind variableKind,
                               TypeSymbol type,
                               BoundConstant? constant)
            : base(syntax, name, type, isReadOnly ? constant : null)
        {
            IsReadOnly = isReadOnly;
            IsStatic = isStatic;
            VariableKind = variableKind;
        }

        internal static VariableSymbol New(SyntaxNode syntax,
                                           string name,
                                           bool isReadOnly,
                                           bool isStatic,
                                           VariableKind variableKind,
                                           TypeSymbol type,
                                           BoundConstant? constant = null)
        {
            return new VariableSymbol(syntax, name, isReadOnly, isStatic, variableKind, type, constant);
        }

        internal static VariableSymbol Local(SyntaxNode syntax,
                                             string name,
                                             bool isReadOnly,
                                             TypeSymbol type,
                                             BoundConstant? constant = null)
        {
            return new VariableSymbol(syntax, name, isReadOnly, false, VariableKind.Local, type, constant);
        }

        internal static VariableSymbol Parameter(SyntaxNode? syntax,
                                                 string name,
                                                 bool isReadOnly,
                                                 TypeSymbol type,
                                                 BoundConstant? constant = null)
        {
            return new VariableSymbol(syntax, name, isReadOnly, false, VariableKind.Parameter, type, constant);
        }
    }
}