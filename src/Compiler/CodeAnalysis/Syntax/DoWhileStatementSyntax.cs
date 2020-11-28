namespace Compiler.CodeAnalysis.Syntax
{
    public partial class DoWhileStatementSyntax : StatementSyntax
    {
        public SyntaxToken DoKeyword { get; }
        public StatementSyntax Body { get; }
        public SyntaxToken WhileKeyword { get; }
        public ExpressionSyntax Condition { get; }
        public override SyntaxKind Kind => SyntaxKind.DoWhileStatement;

        internal DoWhileStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken doKeyword, 
            StatementSyntax body, 
            SyntaxToken whileKeyword, 
            ExpressionSyntax condition)
            : base(syntaxTree)
        {
            DoKeyword = doKeyword;
            Body = body;
            WhileKeyword = whileKeyword;
            Condition = condition;
        }
    }
}