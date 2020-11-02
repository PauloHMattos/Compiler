using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public object Value { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }

        public BoundLiteralExpression(object value)
        {
            Value = value;
            Type = TypeSymbol.GetSymbolFrom(value);
        }
    }
}