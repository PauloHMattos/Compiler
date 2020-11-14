namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class ParenthesizedExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenthesisToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken CloseParenthesisToken { get; }
        public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

        public ParenthesizedExpressionSyntax(SyntaxTree syntaxTree, 
            SyntaxToken openParenthesisToken, 
            ExpressionSyntax expression, 
            SyntaxToken closeParenthesisToken)
            : base(syntaxTree)
        {
            OpenParenthesisToken = openParenthesisToken;
            Expression = expression;
            CloseParenthesisToken = closeParenthesisToken;
        }
    }
}