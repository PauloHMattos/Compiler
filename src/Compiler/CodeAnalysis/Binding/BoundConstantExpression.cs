using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal abstract class BoundConstantExpression : BoundExpression
    {
        public object Value => ConstantValue.Value;
        public override BoundConstant ConstantValue { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }

        public BoundConstantExpression(SyntaxNode syntax, TypeSymbol type, object? value)
            : base(syntax)
        {
            Type = type;
            ConstantValue = new BoundConstant(value!);
        }
        
        public BoundConstantExpression(SyntaxNode syntax, TypeSymbol type)
            : this(syntax, type, type.DefaultValue)
        {
        }
    }
}