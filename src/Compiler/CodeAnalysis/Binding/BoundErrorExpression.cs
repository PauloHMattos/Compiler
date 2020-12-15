using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundErrorExpression : BoundConstantExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;

        public BoundErrorExpression(SyntaxNode syntax, TypeSymbol? type = null)
            : base(syntax, type ?? TypeSymbol.Error)
        {

        }
    }
}