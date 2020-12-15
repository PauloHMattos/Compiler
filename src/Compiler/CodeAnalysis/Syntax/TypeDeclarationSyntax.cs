namespace Compiler.CodeAnalysis.Syntax
{
    public abstract class TypeDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken TypeKeyword { get; }
        public SyntaxToken Identifier { get; }
        public MemberBlockStatementSyntax Body { get; }
        public abstract TypeDeclarationKind TypeKind { get; }
        public override SyntaxKind Kind => SyntaxKind.TypeDeclaration;

        private protected TypeDeclarationSyntax(SyntaxTree syntaxTree,
                                                SyntaxToken keyword,
                                                SyntaxToken identifier,
                                                MemberBlockStatementSyntax body)
            : base(syntaxTree)
        {
            TypeKeyword = keyword;
            Identifier = identifier;
            Body = body;
        }
    }
}