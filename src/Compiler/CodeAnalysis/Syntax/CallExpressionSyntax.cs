namespace Compiler.CodeAnalysis.Syntax
{
    public partial class CallExpressionSyntax : NameExpressionSyntax
    {
        public override SyntaxToken IdentifierToken => base.IdentifierToken;
        public SyntaxToken OpenParenthesisToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenthesisToken { get; }
        public override SyntaxKind Kind => SyntaxKind.CallExpression;

        internal CallExpressionSyntax(SyntaxTree syntaxTree, 
            SyntaxToken identifier, 
            SyntaxToken openParenthesisToken, 
            SeparatedSyntaxList<ExpressionSyntax> arguments, 
            SyntaxToken closeParenthesisToken)
            : base(syntaxTree, identifier)
        {
            OpenParenthesisToken = openParenthesisToken;
            Arguments = arguments;
            CloseParenthesisToken = closeParenthesisToken;
        }
    }
}