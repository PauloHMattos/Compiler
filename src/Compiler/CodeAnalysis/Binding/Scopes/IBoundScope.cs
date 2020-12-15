using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding.Scopes
{
    internal interface IBoundScope
    {
        IBoundScope? Parent { get; }
        DiagnosticBag Diagnostics { get; }
        Symbol? TryLookupSymbol(string name);
        T? TryLookupSymbol<T>(string name) where T : Symbol;
        ImmutableArray<T> GetDeclaredSymbols<T>() where T : Symbol;
        bool TryDeclareVariable(VariableSymbol variable);
        bool TryDeclareFunction(FunctionSymbol function);
        bool TryDeclareType(TypeSymbol typeSymbol);
    }
}