namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class VariableDeclarationStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; }
        public SyntaxToken Identifier { get; }
        public TypeClauseSyntax? TypeClause { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Initializer { get; }
        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

        public VariableDeclarationStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken keyword, 
            SyntaxToken identifier, 
            TypeClauseSyntax? typeClause, 
            SyntaxToken equalsToken, 
            ExpressionSyntax initializer)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Identifier = identifier;
            TypeClause = typeClause;
            EqualsToken = equalsToken;
            Initializer = initializer;
        }
    }
}