namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class EnumSyntax : SyntaxNode
    {
        public SyntaxToken Identifier { get; }
        public EnumValueClauseSyntax? ValueClause { get; }
        public override SyntaxKind Kind => SyntaxKind.EnumValue;

        internal EnumSyntax(SyntaxTree syntaxTree, 
            SyntaxToken identifier, 
            EnumValueClauseSyntax? valueClause)
            : base(syntaxTree)
        {
            Identifier = identifier;
            ValueClause = valueClause;
        }
    }

    public sealed partial class EnumValueClauseSyntax : SyntaxNode
    {
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.EnumValueClause;
        
        internal EnumValueClauseSyntax(SyntaxTree syntaxTree, 
            SyntaxToken equalsToken,
            ExpressionSyntax expression)
            : base(syntaxTree)
        {
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}