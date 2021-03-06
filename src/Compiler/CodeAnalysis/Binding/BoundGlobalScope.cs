﻿using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<TypeSymbol> Types { get; }

        public BoundGlobalScope(ImmutableArray<Diagnostic> diagnostics,
                                FunctionSymbol? mainFunction,
                                ImmutableArray<FunctionSymbol> functions,
                                ImmutableArray<TypeSymbol> types)
        {
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            Functions = functions;
            Types = types;
        }
    }
}