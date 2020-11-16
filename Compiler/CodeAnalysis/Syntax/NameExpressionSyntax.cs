namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class NameExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; }
        public override SyntaxKind Kind => SyntaxKind.NameExpression;

        internal NameExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
        }
    }
}