using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundGlobalScope GlobalScope { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public BoundBlockStatement Statement { get; }

        public BoundProgram(BoundGlobalScope globalScope, 
            ImmutableArray<Diagnostic> diagnostics, 
            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions,
            BoundBlockStatement statement)
        {
            GlobalScope = globalScope;
            Diagnostics = diagnostics;
            Functions = functions;
            Statement = statement;
        }
    }
}