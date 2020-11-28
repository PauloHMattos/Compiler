namespace Compiler.CodeAnalysis.Syntax
{
    public partial class NameExpressionSyntax : ExpressionSyntax
    {
        public virtual SyntaxToken IdentifierToken { get; }
        public override SyntaxKind Kind => SyntaxKind.NameExpression;

        internal NameExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
        }
    }
}