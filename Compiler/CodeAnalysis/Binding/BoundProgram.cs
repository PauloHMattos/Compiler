using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram Previous { get; }
        public BoundGlobalScope GlobalScope { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public BoundBlockStatement Statement { get; }

        public BoundProgram(BoundProgram previous, 
            BoundGlobalScope globalScope, 
            ImmutableArray<Diagnostic> diagnostics, 
            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions,
            BoundBlockStatement statement)
        {
            Previous = previous;
            GlobalScope = globalScope;
            Diagnostics = diagnostics;
            Functions = functions;
            Statement = statement;
        }
    }
}