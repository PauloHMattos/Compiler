using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol VariableSymbol { get; }
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;

        public override TypeSymbol Type => VariableSymbol.Type;

        public BoundVariableExpression(VariableSymbol variableSymbol)
        {
            VariableSymbol = variableSymbol;
        }
    }
}