using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{

    internal sealed class BoundLiteralExpression : BoundConstantExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;

        public BoundLiteralExpression(SyntaxNode syntax, TypeSymbol type, object value)
            : base(syntax, type, value)
        {
        }
        
        public BoundLiteralExpression(SyntaxNode syntax, object value)
            : this(syntax, TypeSymbol.GetSymbolFrom(value), value)
        {
        }
    }
}