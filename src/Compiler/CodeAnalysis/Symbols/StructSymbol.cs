using System.Collections.Immutable;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class StructSymbol : TypeSymbol
    {
        public ImmutableArray<ParameterSymbol> CtorParameters { get; }

        public override SymbolKind Kind => SymbolKind.Struct;
        
        internal StructSymbol(string name,
                              ImmutableArray<ParameterSymbol> ctorParameters,
                              StructDeclarationSyntax? declaration) 
            : base(name, null, typeof(System.ValueType), declaration)
        {
            CtorParameters = ctorParameters;
        }
    }
}