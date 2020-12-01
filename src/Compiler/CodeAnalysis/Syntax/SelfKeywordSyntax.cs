namespace Compiler.CodeAnalysis.Syntax
{
    internal sealed partial class SelfKeywordSyntax : NameExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.SelfKeyword;

        internal SelfKeywordSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
            : base(syntaxTree, keyword)
        {
        }
    }
}
