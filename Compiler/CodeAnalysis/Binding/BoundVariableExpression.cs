using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;

        public override TypeSymbol Type => Variable.Type;
        public override BoundConstant ConstantValue => Variable.Constant;

        public BoundVariableExpression(VariableSymbol variable)
        {
            Variable = variable;
        }
    }
}