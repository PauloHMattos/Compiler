using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<EnumSymbol> Enums { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<BoundStatement> Statements { get; }

        public BoundGlobalScope(BoundGlobalScope? previous,
                                ImmutableArray<Diagnostic> diagnostics,
                                FunctionSymbol? mainFunction,
                                ImmutableArray<FunctionSymbol> functions,
                                ImmutableArray<EnumSymbol> enums,
                                ImmutableArray<VariableSymbol> variables,
                                ImmutableArray<BoundStatement> statements)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            Functions = functions;
            Enums = enums;
            Variables = variables;
            Statements = statements;
        }
    }
}