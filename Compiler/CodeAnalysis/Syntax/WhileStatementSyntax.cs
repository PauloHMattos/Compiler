namespace Compiler.CodeAnalysis.Syntax
{
    internal class WhileStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; }
        public ExpressionSyntax Condition { get; }
        public StatementSyntax Body { get; }
        public override SyntaxKind Kind => SyntaxKind.WhileStatement;
        public WhileStatementSyntax(SyntaxToken keyword, ExpressionSyntax condition, StatementSyntax body)
        {
            Keyword = keyword;
            Condition = condition;
            Body = body;
        }
    }
}