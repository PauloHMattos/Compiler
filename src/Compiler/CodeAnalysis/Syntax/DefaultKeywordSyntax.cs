namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class DefaultKeywordSyntax : ExpressionSyntax
    {
        public SyntaxToken Keyword { get; }
        public override SyntaxKind Kind => SyntaxKind.DefaultKeyword;

        internal DefaultKeywordSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
            : base(syntaxTree)
        {
            Keyword = keyword;
        }
    }
}