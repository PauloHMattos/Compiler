namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class StructDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken StructKeyword { get; }
        public SyntaxToken Identifier { get; }
        public MemberBlockStatementSyntax Body { get; }
        public override SyntaxKind Kind => SyntaxKind.StructDeclaration;

        internal StructDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken structKeyword, SyntaxToken identifier, MemberBlockStatementSyntax body)
        : base(syntaxTree)
        {
            StructKeyword = structKeyword;
            Identifier = identifier;
            Body = body;
        }
    }
}