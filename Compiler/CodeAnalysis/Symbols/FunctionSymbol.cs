using System.Collections.Immutable;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
        public FunctionDeclarationSyntax? Declaration { get; }
        public override SymbolKind Kind => SymbolKind.Function;

        internal FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FunctionDeclarationSyntax? declaration = null) 
            : base(name)
        {
            Parameters = parameters;
            Type = type;
            Declaration = declaration;
        }
    }
}