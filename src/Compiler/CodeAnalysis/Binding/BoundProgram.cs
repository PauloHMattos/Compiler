﻿using System.Collections.Immutable;
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
        public ImmutableDictionary<TypeSymbol, BoundBlockStatement> Types { get; }

        public BoundProgram(BoundProgram? previous,
                            ImmutableArray<Diagnostic> diagnostics,
                            FunctionSymbol? mainFunction,
                            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions,
                            ImmutableDictionary<TypeSymbol, BoundBlockStatement> types)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            Functions = functions;
            Types = types;
        }
    }
}