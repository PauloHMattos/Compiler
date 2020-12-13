using System.Collections.Concurrent;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class StructSymbol : TypeSymbol
    {
        public override SymbolKind Kind => SymbolKind.Struct;
        
        internal StructSymbol(string name,
                              StructDeclarationSyntax? declaration) 
            : base(name,
                   null,
                   typeof(System.ValueType),
                   declaration,
                   new ConcurrentBag<MemberSymbol>())
        {
        }
    }
}