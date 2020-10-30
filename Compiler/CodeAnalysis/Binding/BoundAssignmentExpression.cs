using System;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundAssignmentExpression : BoundExpression
    {
        public VariableSymbol VariableSymbol { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
        public override Type Type => VariableSymbol.Type;

        public BoundAssignmentExpression(VariableSymbol variableSymbol, BoundExpression expression)
        {
            VariableSymbol = variableSymbol;
            Expression = expression;
        }
    }
}