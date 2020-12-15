using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class MemberSymbol : TypedSymbol
    {
        public bool IsReadOnly { get; }
        public bool IsStatic { get; }
        public abstract MemberKind MemberKind { get; }
        public override SymbolKind Kind => SymbolKind.Member;

        private protected MemberSymbol(SyntaxNode? syntax,
                                       string name,
                                       bool isReadOnly,
                                       bool isStatic,
                                       TypeSymbol type,
                                       BoundConstant? constant)
            : base(syntax, name, type, isReadOnly ? constant : null)
        {
            IsReadOnly = isReadOnly;
            IsStatic = isStatic;
        }
    }
}