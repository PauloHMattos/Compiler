using System;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol VariableSymbol { get; }
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;

        public override Type Type => VariableSymbol.Type;

        public BoundVariableExpression(VariableSymbol variableSymbol)
        {
            VariableSymbol = variableSymbol;
        }
    }
}