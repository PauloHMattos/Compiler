﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis
{
    public sealed class Compilation
    {
        private readonly Compilation _previous;
        private BoundGlobalScope _globalScope;

        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

        public Compilation(params SyntaxTree[] syntaxTrees)
            : this(null, syntaxTrees)
        {
        }

        private Compilation(Compilation previous, params SyntaxTree[] syntaxTrees)
        {
            _previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

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

        public Compilation ContinueWith(SyntaxTree syntaxTree)
        {
            return new Compilation(this, syntaxTree);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);
            var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();
            
            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null);
            }

            var program = Binder.BindProgram(GlobalScope);

            var appPath = Environment.GetCommandLineArgs()[0];
            var appDirectory = Path.GetDirectoryName(appPath);
            var cfgPath = Path.Combine(appDirectory, "cfg.dot");
            var cfgStatement = !program.Statement.Statements.Any() && program.Functions.Any()
                ? program.Functions.Last().Value
                : program.Statement;
            var cfg = ControlFlowGraph.Create(cfgStatement);
            using (var streamWriter = new StreamWriter(cfgPath))
                cfg.WriteTo(streamWriter);


            if (program.Diagnostics.Any())
            {
                return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);
            }

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            var program = Binder.BindProgram(GlobalScope);
            if (program.Statement.Statements.Any())
            {
                program.Statement.WriteTo(writer);
            }
            else
            {
                foreach (var (function, statement) in program.Functions)
                {
                    if (!GlobalScope.Functions.Contains(function))
                        continue;

                    function.WriteTo(writer);
                    statement.WriteTo(writer);
                }
            }
        }
    }
}
