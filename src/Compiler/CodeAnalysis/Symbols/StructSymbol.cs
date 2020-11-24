using System.Collections.Immutable;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class StructSymbol : TypeSymbol
    {
        public StructDeclarationSyntax? Declaration { get; }
        public ImmutableArray<ParameterSymbol> CtorParameters { get; }

        public override SymbolKind Kind => SymbolKind.Struct;
        
        internal StructSymbol(string name,
                              ImmutableArray<ParameterSymbol> ctorParameters,
                              ImmutableArray<MemberSymbol> members,
                              StructDeclarationSyntax? declaration = null) 
            : base(name, null, typeof(System.ValueType), members)
        {
            Declaration = declaration;
            CtorParameters = ctorParameters;
        }
    }
}