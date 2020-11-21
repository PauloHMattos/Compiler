using System;
using System.Collections.Immutable;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class EnumSymbol : TypeSymbol
    {
        public ImmutableArray<EnumValueSymbol> Values { get; }
        public EnumDeclarationSyntax Declaration { get; }
        public override SymbolKind Kind => SymbolKind.Enum;

        internal EnumSymbol(string name, ImmutableArray<EnumValueSymbol> values, EnumDeclarationSyntax declaration) 
            : base(name, values[0] ?? null, typeof(Enum))
        {
            Values = values;
            Declaration = declaration;
        }
    }
}