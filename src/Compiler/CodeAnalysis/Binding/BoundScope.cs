using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        public BoundScope? Parent { get; }
        private readonly Dictionary<string, Symbol> _symbols;

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
            _symbols = new Dictionary<string, Symbol>();
        }

        public Symbol? TryLookupSymbol(string name)
        {
            if (_symbols.TryGetValue(name, out var symbol))
            {
                return symbol;
            }

            return Parent?.TryLookupSymbol(name);
        }

        public bool TryDeclareVariable(VariableSymbol variable) => TryDeclareSymbol(variable, out var _);
        public bool TryDeclareStruct(StructSymbol structSymbol) => TryDeclareSymbol(structSymbol, out var _);
        public bool TryDeclareFunction(FunctionSymbol function)
        {
            if (!TryDeclareSymbol(function, out var alreadyDeclaredSymbol))
            {
                if (alreadyDeclaredSymbol is FunctionSymbol f)
                {
                    if (f.SameSignature(function))
                    {
                        return false;
                    }

                    var overloads = f.Overloads.ToBuilder();
                    overloads.Add(function);
                    _symbols[f.Name] = new FunctionSymbol(f.Name,
                                                          f.Parameters,
                                                          f.Type,
                                                          overloads.ToImmutable(),
                                                          f.Declaration);
                    return true;
                }
                return false;
            }
            return true;
        }

        public bool TryDeclareEnum(EnumSymbol enumSymbol) => TryDeclareSymbol(enumSymbol, out var _);

        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => GetDeclaredSymbols<VariableSymbol>();

        public ImmutableArray<FunctionSymbol> GetDeclaredFunctions() => GetDeclaredSymbols<FunctionSymbol>();
        public ImmutableArray<EnumSymbol> GetDeclaredEnums() => GetDeclaredSymbols<EnumSymbol>();
        public ImmutableArray<StructSymbol> GetDeclaredStructs() => GetDeclaredSymbols<StructSymbol>();

        private bool TryDeclareSymbol<TSymbol>(TSymbol symbol, out Symbol? alreadyDeclaredSymbol) where TSymbol : Symbol
        {
            if (_symbols.TryGetValue(symbol.Name, out alreadyDeclaredSymbol))
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