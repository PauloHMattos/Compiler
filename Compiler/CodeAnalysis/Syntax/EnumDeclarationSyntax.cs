namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class EnumDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken FunctionKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenBraceToken { get; }
        public SeparatedSyntaxList<EnumSyntax> Values { get; }
        public SyntaxToken CloseBraceToken { get; }
        public override SyntaxKind Kind => SyntaxKind.EnumDeclaration;

        internal EnumDeclarationSyntax(SyntaxTree syntaxTree, 
            SyntaxToken functionKeyword, 
            SyntaxToken identifier, 
            SyntaxToken openBraceToken, 
            SeparatedSyntaxList<EnumSyntax> values, 
            SyntaxToken closeBraceToken)
            : base(syntaxTree)
        {
            FunctionKeyword = functionKeyword;
            Identifier = identifier;
            OpenBraceToken = openBraceToken;
            Values = values;
            CloseBraceToken = closeBraceToken;
        }
    }
}