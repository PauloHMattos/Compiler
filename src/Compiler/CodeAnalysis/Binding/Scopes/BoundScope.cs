using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding.Scopes
{
    internal abstract class BoundScope : IBoundScope
    {
        public IBoundScope? Parent { get; }
        private readonly Dictionary<string, Symbol> _symbols;
        public DiagnosticBag Diagnostics { get; }

        private protected BoundScope(IBoundScope? parent, DiagnosticBag diagnostics)
        {
            Parent = parent;
            Diagnostics = diagnostics;
            _symbols = new Dictionary<string, Symbol>();
        }

        public Symbol? TryLookupSymbol(string name)
        {
            return TryLookupSymbol<Symbol>(name);
        }

        public T? TryLookupSymbol<T>(string name) where T : Symbol
        {
            if (_symbols.TryGetValue(name, out var symbol) && symbol is T castSymbol)
            {
                return castSymbol;
            }
            return Parent?.TryLookupSymbol<T>(name);
        }

        public ImmutableArray<TSymbol> GetDeclaredSymbols<TSymbol>() where TSymbol : Symbol
        {
            var builder = ImmutableArray.CreateBuilder<TSymbol>();
            foreach (var entry in _symbols)
            {
                if (entry.Value is TSymbol s)
                {
                    builder.Add(s);
                }
            }
            return builder.ToImmutableArray();
        }

        public virtual bool TryDeclareVariable(VariableSymbol variable) => TryDeclareSymbol(variable, out var _);
        public virtual bool TryDeclareFunction(FunctionSymbol function)
        {
            if (!TryDeclareSymbol(function, out var alreadyDeclaredSymbol, false))
            {
                if (alreadyDeclaredSymbol is FunctionSymbol f)
                {
                    if (f.SameSignature(function))
                    {
                        ReportSymbolAlreadyDeclared(function);
                        return false;
                    }

                    f.OverloadsBuilder.Add(function);
                    return true;
                }
                else
                {
                    ReportSymbolAlreadyDeclared(function);
                }
                return false;
            }
            return true;
        }
        public virtual bool TryDeclareType(TypeSymbol typeSymbol)
        {
            Debug.Assert(typeSymbol.Declaration != null);
            return TryDeclareSymbol(typeSymbol, out var _);
        }

        protected bool TryDeclareSymbol<TSymbol>(TSymbol symbol, out Symbol? alreadyDeclaredSymbol, bool report = true) where TSymbol : Symbol
        {
            if (_symbols.TryGetValue(symbol.Name, out alreadyDeclaredSymbol))
            {
                if (report)
                {
                    ReportSymbolAlreadyDeclared(symbol);
                }
                return false;
            }
            _symbols.Add(symbol.Name, symbol);
            return true;
        }

        protected virtual void ReportSymbolAlreadyDeclared(Symbol symbol)
        {
            Debug.Assert(symbol.Syntax != null);
            Diagnostics.ReportSymbolAlreadyDeclared(symbol.Syntax.Location, symbol.Name);
        }
    }
}