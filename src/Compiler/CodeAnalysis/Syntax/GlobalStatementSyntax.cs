namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class GlobalStatementSyntax : MemberSyntax
    {
        public StatementSyntax Statement { get; }
        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;

        internal GlobalStatementSyntax(SyntaxTree syntaxTree, StatementSyntax statement)
            : base(syntaxTree)
        {
            Statement = statement;
        }
    }
}