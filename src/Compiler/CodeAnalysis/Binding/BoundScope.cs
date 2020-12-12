using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

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
                                                          f.ReturnType,
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
        public ImmutableArray<TypeSymbol> GetDeclaredTypes() => GetDeclaredSymbols<TypeSymbol>();

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

        internal void TryDeclareType(TypeSymbol s)
        {
            Debug.Assert(s.Declaration != null);
            switch (s.Declaration.TypeKind)
            {
                case TypeDeclarationKind.Enum:
                    TryDeclareEnum((EnumSymbol)s);
                    break;

                case TypeDeclarationKind.Struct:
                    TryDeclareStruct((StructSymbol)s);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected declaration kind {s.Declaration.TypeKind}");
            }
        }
    }
}