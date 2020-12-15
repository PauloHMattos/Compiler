using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public ImmutableArray<TypeSymbol> Types { get; }

        public BoundProgram(ImmutableArray<Diagnostic> diagnostics,
                            FunctionSymbol? mainFunction,
                            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions,
                            ImmutableArray<TypeSymbol> types)
        {
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            Functions = functions;
            Types = types;
        }
    }
}