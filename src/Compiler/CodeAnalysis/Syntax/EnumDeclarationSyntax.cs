namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class EnumValuesStatementSyntax : StatementSyntax
    {
        public SeparatedSyntaxList<EnumSyntax> Values { get; }
        public override SyntaxKind Kind => SyntaxKind.EnumElementDeclarationStatement;

        public EnumValuesStatementSyntax(SyntaxTree syntaxTree,
                                         SeparatedSyntaxList<EnumSyntax> values)
            : base(syntaxTree)
        {
            Values = values;
        }
    }

    public sealed partial class EnumDeclarationSyntax : TypeDeclarationSyntax
    {
        public override TypeDeclarationKind TypeKind => TypeDeclarationKind.Enum;

        internal EnumDeclarationSyntax(SyntaxTree syntaxTree,
                                         SyntaxToken enumKeyword,
                                         SyntaxToken identifier,
                                         MemberBlockStatementSyntax body)
            : base(syntaxTree, enumKeyword, identifier, body)
        {
        }
    }
}