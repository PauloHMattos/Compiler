using System.Collections.Generic;
using System.Collections.Immutable;
using Compiler.CodeAnalysis.Binding.Scopes;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : MemberSymbol
    {
        public ImmutableArray<VariableSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }
        public FunctionDeclarationSyntax? Declaration { get; }
        public TypeSymbol? Receiver { get; }
        public override SymbolKind Kind => SymbolKind.Function;
        public override MemberKind MemberKind => MemberKind.Method;

        internal List<FunctionSymbol> OverloadsBuilder { get; }
        private ImmutableArray<FunctionSymbol> _overloads;
        public ImmutableArray<FunctionSymbol> Overloads
        {
            get
            {
                if (_overloads == null)
                {
                    _overloads = OverloadsBuilder.ToImmutableArray();
                }
                return _overloads;
            }
        }

        internal FunctionBoundScope? BoundScope { get; }

        internal FunctionSymbol(string name,
                                ImmutableArray<VariableSymbol> parameters,
                                TypeSymbol type,
                                FunctionDeclarationSyntax? declaration = null,
                                IBoundScope? parentScope = null,
                                TypeSymbol? receiver = null)
            : base(declaration?.Identifier, name, true, false, type, null)
        {
            Parameters = parameters;
            ReturnType = type;
            Declaration = declaration;
            Receiver = receiver;
            OverloadsBuilder = new List<FunctionSymbol>();
            
            if (parentScope!= null)
            {
                BoundScope = new FunctionBoundScope(receiver, this, parentScope);
            }
        }
    }
}