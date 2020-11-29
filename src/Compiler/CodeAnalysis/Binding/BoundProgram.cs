using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public ImmutableArray<EnumSymbol> Enums { get; }
        public ImmutableDictionary<StructSymbol, BoundBlockStatement> Structs { get; }

        public BoundProgram(BoundProgram? previous,
                            ImmutableArray<Diagnostic> diagnostics,
                            FunctionSymbol? mainFunction,
                            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions,
                            ImmutableArray<EnumSymbol> enums,
                            ImmutableDictionary<StructSymbol, BoundBlockStatement> structs)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            Functions = functions;
            Enums = enums;
            Structs = structs;
        }
    }
}