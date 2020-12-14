using System.Collections.Immutable;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : MemberSymbol
    {
        public ImmutableArray<VariableSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }
        public FunctionDeclarationSyntax? Declaration { get; }
        public TypeSymbol? Receiver { get; }
        public ImmutableArray<FunctionSymbol> Overloads { get; }
        public override SymbolKind Kind => SymbolKind.Function;
        public override MemberKind MemberKind => MemberKind.Method;

        internal FunctionSymbol(string name,
                                ImmutableArray<VariableSymbol> parameters,
                                TypeSymbol type,
                                ImmutableArray<FunctionSymbol> overloads,
                                FunctionDeclarationSyntax? declaration = null,
                                TypeSymbol? receiver = null)
            : base(name, true, false, type, null)
        {
            Parameters = parameters;
            ReturnType = type;
            Overloads = overloads;
            Declaration = declaration;
            Receiver = receiver;
        }

        
        internal FunctionSymbol(string name,
                                ImmutableArray<VariableSymbol> parameters,
                                TypeSymbol type,
                                FunctionDeclarationSyntax? declaration = null)
            : this(name, parameters, type, ImmutableArray<FunctionSymbol>.Empty, declaration)
        {
        }
    }
}