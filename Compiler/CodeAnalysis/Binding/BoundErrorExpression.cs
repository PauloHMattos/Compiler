using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundErrorExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
        public override TypeSymbol Type => TypeSymbol.Error;
    }
}