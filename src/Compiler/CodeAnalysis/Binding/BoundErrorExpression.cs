using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundErrorExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
        public override TypeSymbol Type => TypeSymbol.Error;

        public BoundErrorExpression(SyntaxNode syntax)
            : base(syntax)
        {
        }
    }
}