namespace Compiler.CodeAnalysis.Syntax
{
    internal class ContinueStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; }
        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;

        public ContinueStatementSyntax(SyntaxToken keyword)
        {
            Keyword = keyword;
        }
    }
}