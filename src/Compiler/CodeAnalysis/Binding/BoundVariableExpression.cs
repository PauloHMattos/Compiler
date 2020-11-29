using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public bool ByReference { get; }
        public override TypeSymbol Type => Variable.Type;
        public override BoundConstant? ConstantValue => Variable.Constant;

        public BoundVariableExpression(SyntaxNode syntax, VariableSymbol variable, bool byReference)
            : base(syntax)
        {
            Variable = variable;
            ByReference = byReference;
        }
    }
}