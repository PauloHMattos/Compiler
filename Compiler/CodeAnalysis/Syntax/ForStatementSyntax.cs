namespace Compiler.CodeAnalysis.Syntax
{
    internal sealed partial class ForStatementSyntax : StatementSyntax
    {
        public SyntaxToken ForKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax LowerBound { get; }
        public SyntaxToken ToKeyword { get; }
        public ExpressionSyntax UpperBound { get; }
        public StatementSyntax Body { get; }
        public StepClauseSyntax StepClause { get; }
        public override SyntaxKind Kind => SyntaxKind.ForStatement;

        public ForStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken forKeyword, 
            SyntaxToken identifier,
            SyntaxToken equalsToken, 
            ExpressionSyntax lowerBound, 
            SyntaxToken toKeyword, 
            ExpressionSyntax upperBound,
            StatementSyntax body,
            StepClauseSyntax stepClause)
            : base(syntaxTree)
        {
            ForKeyword = forKeyword;
            Identifier = identifier;
            EqualsToken = equalsToken;
            LowerBound = lowerBound;
            ToKeyword = toKeyword;
            UpperBound = upperBound;
            Body = body;
            StepClause = stepClause;
        }
    }
}