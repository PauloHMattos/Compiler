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
        public StepClauseSyntax? StepClause { get; }
        public StatementSyntax Body { get; }
        public override SyntaxKind Kind => SyntaxKind.ForStatement;

        public ForStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken forKeyword, 
            SyntaxToken identifier,
            SyntaxToken equalsToken, 
            ExpressionSyntax lowerBound, 
            SyntaxToken toKeyword, 
            ExpressionSyntax upperBound,
            StepClauseSyntax? stepClause,
            StatementSyntax body)
            : base(syntaxTree)
        {
            ForKeyword = forKeyword;
            Identifier = identifier;
            EqualsToken = equalsToken;
            LowerBound = lowerBound;
            ToKeyword = toKeyword;
            UpperBound = upperBound;
            StepClause = stepClause;
            Body = body;
        }
    }
}