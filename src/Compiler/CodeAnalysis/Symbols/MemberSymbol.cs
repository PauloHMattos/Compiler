using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class MemberSymbol : TypedSymbol
    {
        public bool IsReadOnly { get; }
        public bool IsStatic { get; }
        // TODO - HACK:
        // Should not be nullable
        // For now it's because functions can be global statements
        // In the future they will only be valid inside classes/structs
        // and this will be fixed
        public TypeSymbol? ReceiverType { get; }
        public abstract MemberKind MemberKind { get; }
        public override SymbolKind Kind => SymbolKind.Member;

        private protected MemberSymbol(SyntaxNode? syntax,
                                       string name,
                                       bool isReadOnly,
                                       bool isStatic,
                                       TypeSymbol? receiverType,
                                       TypeSymbol type,
                                       BoundConstant? constant)
            : base(syntax, name, type, isReadOnly ? constant : null)
        {
            IsReadOnly = isReadOnly;
            IsStatic = isStatic;
            ReceiverType = receiverType;
        }
    }
}