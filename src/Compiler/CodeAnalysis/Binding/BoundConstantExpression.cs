using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal abstract class BoundConstantExpression : BoundExpression
    {
        public object Value => ConstantValue.Value;
        public override BoundConstant ConstantValue { get; }
        public override TypeSymbol Type { get; }

        private protected BoundConstantExpression(SyntaxNode syntax, TypeSymbol type, object? value)
            : base(syntax)
        {
            Type = type;
            ConstantValue = new BoundConstant(value!);
        }
        
        private protected BoundConstantExpression(SyntaxNode syntax, TypeSymbol type)
            : this(syntax, type, type.DefaultValue)
        {
        }
    }
}