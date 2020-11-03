using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        public BoundScope Parent { get; }
        private readonly Dictionary<string, Symbol> _symbols;

        public BoundScope(BoundScope parent)
        {
            Parent = parent;
            _symbols = new Dictionary<string, Symbol>();
        }

        public Symbol TryLookupSymbol(string name)
        {
            if (_symbols.TryGetValue(name, out var symbol))
            {
                return symbol;
            }

            return Parent?.TryLookupSymbol(name);
        }

        public bool TryDeclareVariable(VariableSymbol variable) => TryDeclareSymbol(variable);

        public bool TryDeclareFunction(FunctionSymbol function) => TryDeclareSymbol(function);

        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => GetDeclaredSymbols<VariableSymbol>();

        public ImmutableArray<FunctionSymbol> GetDeclaredFunction() => GetDeclaredSymbols<FunctionSymbol>();

        private bool TryDeclareSymbol<TSymbol>(TSymbol symbol) where TSymbol : Symbol
        {
            if (_symbols.ContainsKey(symbol.Name))
            {
                return false;
            }
            _symbols.Add(symbol.Name, symbol);
            return true;
        }

        private ImmutableArray<TSymbol> GetDeclaredSymbols<TSymbol>() where TSymbol : Symbol
        {
            return _symbols.Values.OfType<TSymbol>().ToImmutableArray();
        }
    }
}