using System;
using System.Collections.Concurrent;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class EnumSymbol : TypeSymbol
    {
        public override SymbolKind Kind => SymbolKind.Enum;

        internal EnumSymbol(string name,
                            EnumDeclarationSyntax declaration) 
            : base(name, 0, typeof(Enum), declaration, new ConcurrentBag<MemberSymbol>())
        {
        }
    }
}