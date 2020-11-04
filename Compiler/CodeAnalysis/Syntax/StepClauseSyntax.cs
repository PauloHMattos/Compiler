namespace Compiler.CodeAnalysis.Syntax
{
    internal sealed class StepClauseSyntax : SyntaxNode
    {
        public SyntaxToken Keyword { get; }
        public ExpressionSyntax Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.StepClause;

        public StepClauseSyntax(SyntaxTree syntaxTree, 
            SyntaxToken keyword, 
            ExpressionSyntax expression)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Expression = expression;
        }
    }
}