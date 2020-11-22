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

        internal EnumSymbol(string name, ImmutableArray<(string, int)> values, EnumDeclarationSyntax declaration) 
            : base(name, values.Length == 0 ? null : values[0].Item2, typeof(Enum))
        {
            var builder = ImmutableArray.CreateBuilder<EnumValueSymbol>();
            foreach (var (identifier, value) in values)
            {
                builder.Add(new EnumValueSymbol(identifier, this, value));
            }
            Values = builder.ToImmutable();
            Declaration = declaration;
        }
    }
}