namespace Compiler.CodeAnalysis.Syntax
{
    internal partial class WhileStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; }
        public ExpressionSyntax Condition { get; }
        public StatementSyntax Body { get; }
        public override SyntaxKind Kind => SyntaxKind.WhileStatement;
        
        public WhileStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken keyword, 
            ExpressionSyntax condition, 
            StatementSyntax body)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Condition = condition;
            Body = body;
        }
    }
}