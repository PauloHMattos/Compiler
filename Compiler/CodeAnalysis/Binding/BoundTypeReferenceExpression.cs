using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundTypeReferenceExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.TypeReferenceExpression;

        public override TypeSymbol Type { get; }

        public BoundTypeReferenceExpression(SyntaxNode syntax, TypeSymbol type)
            : base(syntax)
        {
            Type = type;
        }
    }
}