using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding.Scopes
{
    internal sealed class FunctionBoundScope : BoundScope
    {
        public TypeSymbol? OwnerType { get; }
        public FunctionSymbol OwnerFunction { get; }

        public FunctionBoundScope(TypeSymbol? ownerType, FunctionSymbol ownerFunction, IBoundScope parent)
            : base(parent, new DiagnosticBag())
        {
            OwnerType = ownerType;
            OwnerFunction = ownerFunction;

            foreach (var p in OwnerFunction.Parameters)
            {
                _ = TryDeclareVariable(p);
            }
        }

        public override bool TryDeclareType(TypeSymbol typeSymbol)
        {
            // Not allowed
            return false;
        }

        public override bool TryDeclareFunction(FunctionSymbol function)
        {
            // Not allowed
            return false;
        }
    }
}