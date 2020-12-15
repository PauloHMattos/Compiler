namespace Compiler.CodeAnalysis.Syntax
{
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