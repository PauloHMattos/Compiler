using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundConversionExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.ConversionExpression;
        public override TypeSymbol Type { get; }
        public BoundExpression Expression { get; }

        public BoundConversionExpression(TypeSymbol type, BoundExpression expression)
        {
            Type = type;
            Expression = expression;
        }
    }
}