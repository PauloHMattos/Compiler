using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Emit;

namespace Compiler.CodeAnalysis
{
    public sealed class Compilation
    {
        private readonly Compilation? _previous;
        private BoundGlobalScope? _globalScope;
        public bool IsScript { get; }
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public FunctionSymbol? MainFunction => GlobalScope.MainFunction;
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<EnumSymbol> Enums => GlobalScope.Enums;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(IsScript, _previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }
                return _globalScope;
            }
        }

        private Compilation(bool isScript, Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            IsScript = isScript;
            _previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(false, null, syntaxTrees);
        }

        public static Compilation CreateScript(Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(true, previous, syntaxTrees);
        }

        private BoundProgram GetProgram()
        {
            var previous = _previous?.GetProgram();
            return Binder.BindProgram(IsScript, previous, GlobalScope);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            if (GlobalScope.Diagnostics.HasErrors())
            {
                return new EvaluationResult(GlobalScope.Diagnostics, null);
            }

            var program = GetProgram();

            // var appPath = Environment.GetCommandLineArgs()[0];
            // var appDirectory = Path.GetDirectoryName(appPath);
            // var cfgPath = Path.Combine(appDirectory, "cfg.dot");
            // var cfgStatement = !program.Statement.Statements.Any() && program.Functions.Any()
            //     ? program.Functions.Last().Value
            //     : program.Statement;
            // var cfg = ControlFlowGraph.Create(cfgStatement);
            // using (var streamWriter = new StreamWriter(cfgPath))
            //     cfg.WriteTo(streamWriter);


            if (program.Diagnostics.HasErrors())
            {
                return new EvaluationResult(program.Diagnostics, null);
            }

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.MainFunction != null)
            {
                EmitTree(GlobalScope.MainFunction, writer);
            }
            else if (GlobalScope.ScriptFunction != null)
            {
                EmitTree(GlobalScope.ScriptFunction, writer);
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

                foreach (var enumSymbol in submission.Enums)
                {
                    if (seenSymbolNames.Add(enumSymbol.Name))
                    {
                        yield return enumSymbol;
                    }
                }

                foreach (var variable in submission.Variables)
                {
                    if (seenSymbolNames.Add(variable.Name))
                    {
                        yield return variable;
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
    }
}
