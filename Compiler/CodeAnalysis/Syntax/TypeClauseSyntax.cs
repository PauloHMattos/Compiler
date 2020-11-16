namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class TypeClauseSyntax : SyntaxNode
    {
        public SyntaxToken ColonToken { get; }
        public SyntaxToken Identifier { get; }
        public override SyntaxKind Kind => SyntaxKind.TypeClause;

        internal TypeClauseSyntax(SyntaxTree syntaxTree, 
            SyntaxToken colonToken, 
            SyntaxToken identifier)
            : base(syntaxTree)
        {
            ColonToken = colonToken;
            Identifier = identifier;
        }
    }
}