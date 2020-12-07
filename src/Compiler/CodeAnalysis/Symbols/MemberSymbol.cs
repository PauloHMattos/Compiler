using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class MemberSymbol : TypedSymbol
    {
        public override SymbolKind Kind => SymbolKind.Member;
        public abstract MemberKind MemberKind { get; }

        private protected MemberSymbol(string name, TypeSymbol type, BoundConstant? constant) : base(name, type, constant)
        {
        }
    }
}