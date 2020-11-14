namespace Compiler.CodeAnalysis.Syntax
{
    internal partial class BreakStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; }
        public override SyntaxKind Kind => SyntaxKind.BreakStatement;

        public BreakStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
            : base(syntaxTree)
        {
            Keyword = keyword;
        }
    }
}