namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class ReturnStatementSyntax : StatementSyntax
    {
        public SyntaxToken ReturnKeyword { get; }
        public ExpressionSyntax? Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.ReturnStatement;

        public ReturnStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken returnKeyword, 
            ExpressionSyntax? expression)
            : base(syntaxTree)
        {
            ReturnKeyword = returnKeyword;
            Expression = expression;
        }
    }
}