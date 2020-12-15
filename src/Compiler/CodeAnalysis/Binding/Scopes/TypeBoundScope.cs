using System.Collections.Immutable;
using System.Diagnostics;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding.Scopes
{
    internal sealed class TypeBoundScope : BoundScope
    {
        public TypeSymbol OwnerType { get; }

        public TypeBoundScope(TypeSymbol ownerType, IBoundScope? parent = null)
            : base(parent, new DiagnosticBag())
        {
            OwnerType = ownerType;
        }

        internal ImmutableArray<MemberSymbol> GetMembers()
        {
            return GetDeclaredSymbols<MemberSymbol>();
        }

        internal bool TryDeclareField(FieldSymbol field)
        {
            return TryDeclareSymbol(field, out var _);
        }

        protected override void ReportSymbolAlreadyDeclared(Symbol symbol)
        {
            Debug.Assert(symbol.Syntax != null);
            Diagnostics.ReportMemberAlreadyDeclared(symbol.Syntax.Location, OwnerType.Name, symbol.Name);
        }
    }
}