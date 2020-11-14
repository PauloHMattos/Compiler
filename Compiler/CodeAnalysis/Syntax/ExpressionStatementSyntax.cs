namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class ExpressionStatementSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

        public ExpressionStatementSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression)
            : base(syntaxTree)
        {
            Expression = expression;
        }
    }
}