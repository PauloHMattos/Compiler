using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public object Value => ConstantValue.Value;
        public override BoundConstant ConstantValue { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }

        public BoundLiteralExpression(object value)
        {
            Type = TypeSymbol.GetSymbolFrom(value);
            ConstantValue = new BoundConstant(value);
        }
    }
}