namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class LiteralExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken LiteralToken { get; }
        public object Value { get; }

        public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

        public LiteralExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken literalToken) : 
            this(syntaxTree, literalToken, literalToken.Value!)
        {
        }

        public LiteralExpressionSyntax(SyntaxTree syntaxTree, 
            SyntaxToken literalToken, 
            object value)
            : base(syntaxTree)
        {
            LiteralToken = literalToken;
            Value = value;
        }
    }
}