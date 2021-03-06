using System;
using Compiler.CodeAnalysis.Binding.Scopes;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class EnumSymbol : TypeSymbol
    {
        public override SymbolKind Kind => SymbolKind.Enum;

        internal EnumSymbol(string name,
                            EnumDeclarationSyntax declaration,
                            IBoundScope parentScope) 
            : base(name, 0, typeof(Enum), declaration, parentScope)
        {
        }
    }
}