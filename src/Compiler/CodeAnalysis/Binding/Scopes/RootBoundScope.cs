using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding.Scopes
{
    internal sealed class RootBoundScope : BoundScope
    {
        public RootBoundScope()
            : base(null, new DiagnosticBag())
        {
        }

        public override bool TryDeclareVariable(VariableSymbol variable)
        {
            // Not allowed
            return false;
        }
    }
}