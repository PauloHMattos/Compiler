using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal abstract class BoundExpression : BoundNode
    {
        public abstract TypeSymbol Type { get; }
        public virtual BoundConstant? ConstantValue => null;

        private protected BoundExpression(SyntaxNode syntax)
            : base(syntax)
        {
        }
    }
}