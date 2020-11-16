using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundConversionExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.ConversionExpression;
        public override TypeSymbol Type { get; }
        public BoundExpression Expression { get; }

        public BoundConversionExpression(SyntaxNode syntax, TypeSymbol type, BoundExpression expression)
            : base(syntax)
        {
            Type = type;
            Expression = expression;
        }
    }
}