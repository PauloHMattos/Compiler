namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class StepClauseSyntax : SyntaxNode
    {
        public SyntaxToken Keyword { get; }
        public ExpressionSyntax Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.StepClause;

        internal StepClauseSyntax(SyntaxTree syntaxTree, 
            SyntaxToken keyword, 
            ExpressionSyntax expression)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Expression = expression;
        }
    }
}