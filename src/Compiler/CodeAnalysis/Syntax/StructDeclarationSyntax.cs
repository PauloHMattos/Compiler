namespace Compiler.CodeAnalysis.Syntax
{
    public enum TypeDeclarationKind
    {
        Enum,
        Struct
    }

    public abstract class TypeDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken TypeKeyword { get; }
        public SyntaxToken Identifier { get; }
        public MemberBlockStatementSyntax Body { get; }
        public abstract TypeDeclarationKind TypeKind { get; }
        public override SyntaxKind Kind => SyntaxKind.TypeDeclaration;

        internal TypeDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, SyntaxToken identifier, MemberBlockStatementSyntax body)
        : base(syntaxTree)
        {
            TypeKeyword = keyword;
            Identifier = identifier;
            Body = body;
        }
    }

    public sealed partial class StructDeclarationSyntax : TypeDeclarationSyntax
    {
        public override TypeDeclarationKind TypeKind => TypeDeclarationKind.Struct;

        internal StructDeclarationSyntax(SyntaxTree syntaxTree,
                                         SyntaxToken structKeyword,
                                         SyntaxToken identifier,
                                         MemberBlockStatementSyntax body)
            : base(syntaxTree, structKeyword, identifier, body)
        {
        }
    }
}