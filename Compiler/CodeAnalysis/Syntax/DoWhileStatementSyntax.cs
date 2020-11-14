namespace Compiler.CodeAnalysis.Syntax
{
    internal partial class DoWhileStatementSyntax : StatementSyntax
    {
        public SyntaxToken DoKeyword { get; }
        public StatementSyntax Body { get; }
        public SyntaxToken WhileKeyword { get; }
        public ExpressionSyntax Condition { get; }
        public override SyntaxKind Kind => SyntaxKind.DoWhileStatement;

        public DoWhileStatementSyntax(SyntaxTree syntaxTree, 
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