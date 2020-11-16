using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public object Value => ConstantValue.Value;
        public override BoundConstant ConstantValue { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }

        public BoundLiteralExpression(SyntaxNode syntax, object value)
            : base(syntax)
        {
            Type = TypeSymbol.GetSymbolFrom(value);
            ConstantValue = new BoundConstant(value);
        }
    }
}