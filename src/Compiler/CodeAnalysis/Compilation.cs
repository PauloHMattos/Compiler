using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Emit;
using System;
using System.Linq;
using Compiler.CodeAnalysis.Binding.FlowControl;

namespace Compiler.CodeAnalysis
{
    public sealed class Compilation
    {
        private readonly Compilation? _previous;
        private BoundGlobalScope? _globalScope;
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public FunctionSymbol? MainFunction => GlobalScope.MainFunction;
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<TypeSymbol> Types => GlobalScope.Types;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(_previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }
                return _globalScope;
            }
        }

        private Compilation(Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            _previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(null, syntaxTrees);
        }


        public ImmutableArray<Diagnostic> Validate(bool generateGraph)
        {
            var program = GetProgram();
            if (generateGraph)
            {
                GenerateFlowGraph(program);
            }
            return program.Diagnostics;
        }

        private BoundProgram GetProgram()
        {
            var previous = _previous?.GetProgram();
            return Binder.BindProgram(previous, GlobalScope);
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.MainFunction != null)
            {
                EmitTree(GlobalScope.MainFunction, writer);
            }
        }

        public void EmitTree(FunctionSymbol symbol, TextWriter writer)
        {
            var program = GetProgram();
            symbol.WriteTo(writer);
            writer.WriteLine();
            if (!program.Functions.TryGetValue(symbol, out var body))
            {
                return;
            }
            body.WriteTo(writer);
        }

        public IEnumerable<Symbol> GetSymbols()
        {
            var submission = this;
            var seenSymbolNames = new HashSet<string>();

            while (submission != null)
            {
                foreach (var function in submission.Functions)
                {
                    if (seenSymbolNames.Add(function.Name))
                    {
                        yield return function;
                    }
                }

                foreach (var typeSymbol in submission.Types)
                {
                    if (seenSymbolNames.Add(typeSymbol.Name))
                    {
                        yield return typeSymbol;
                    }
                }
                
                submission = submission._previous;
            }
        }

        public ImmutableArray<Diagnostic> Emit(string moduleName, IEnumerable<string> references, string outputPath)
        {
            if (GlobalScope.Diagnostics.HasErrors())
            {
                return GlobalScope.Diagnostics;
            }
            var program = GetProgram();
            var emittionDiagnostics = Emitter.Emit(program, moduleName, references, outputPath);

            var accumulatedDiagnostics = emittionDiagnostics.ToBuilder();
            accumulatedDiagnostics.AddRange(GlobalScope.Diagnostics);
            return accumulatedDiagnostics.ToImmutable();
        }

        internal static void GenerateFlowGraph(BoundProgram program)
        {
            var function = program.Functions.FirstOrDefault();
            if (function.Value == null)
            {
                return;
            }
            var appPath = Environment.CurrentDirectory;
            var appDirectory = Path.GetDirectoryName(appPath);
            var cfgPath = Path.Combine(appDirectory!, "cfg.dot");
            var diagnostics = new DiagnosticBag();
            var cfg = ControlFlowGraph.Create(function.Value, diagnostics);

            using var streamWriter = new StreamWriter(cfgPath);
            cfg.WriteTo(streamWriter);
        }
    }
}
