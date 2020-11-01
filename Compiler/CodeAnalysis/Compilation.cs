using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis
{
    public sealed class Compilation
    {
        private readonly Compilation _previous;
        private BoundGlobalScope _globalScope;

        public SyntaxTree SyntaxTree { get; }

        public Compilation(SyntaxTree syntaxTree)
            : this(null, syntaxTree)
        {
        }

        private Compilation(Compilation previous, SyntaxTree syntaxTree)
        {
            _previous = previous;
            SyntaxTree = syntaxTree;
        }

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(_previous?.GlobalScope, SyntaxTree.Root);
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
            var boundExpression = GlobalScope.Statement;

            var evaluator = new Evaluator(boundExpression, variables);

            var diagnostics = SyntaxTree.Diagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null);
            }

            var value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic.Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            GlobalScope.Statement.WriteTo(writer);
        }
    }
}
