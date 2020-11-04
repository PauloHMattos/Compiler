namespace Compiler.CodeAnalysis.Syntax
{
    internal class BreakStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; }
        public override SyntaxKind Kind => SyntaxKind.BreakStatement;

        public BreakStatementSyntax(SyntaxToken keyword)
        {
            Keyword = keyword;
        }
    }
}