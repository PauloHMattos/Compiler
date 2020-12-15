using System.Collections.Immutable;
using Compiler.CodeAnalysis.Binding.Scopes;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class StructSymbol : TypeSymbol
    {
        public override SymbolKind Kind => SymbolKind.Struct;
        
        internal StructSymbol(string name,
                              StructDeclarationSyntax declaration,
                              IBoundScope parentScope) 
            : base(name,
                   null,
                   typeof(System.ValueType),
                   declaration,
                   parentScope)
        {
            BoundScope!.TryDeclareFunction(new FunctionSymbol(".ctor",
                                                               ImmutableArray<VariableSymbol>.Empty,
                                                               this,
                                                               null,
                                                               BoundScope,
                                                               this));
        }
    }
}