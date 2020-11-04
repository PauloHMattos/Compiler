namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class TypeClauseSyntax : SyntaxNode
    {
        public SyntaxToken ColonToken { get; }
        public SyntaxToken Identifier { get; }
        public override SyntaxKind Kind => SyntaxKind.TypeClause;

        public TypeClauseSyntax(SyntaxTree syntaxTree, 
            SyntaxToken colonToken, 
            SyntaxToken identifier)
            : base(syntaxTree)
        {
            ColonToken = colonToken;
            Identifier = identifier;
        }
    }
}