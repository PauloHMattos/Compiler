namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class GlobalStatementSyntax : MemberSyntax
    {
        public StatementSyntax Statement { get; }
        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;

        public GlobalStatementSyntax(StatementSyntax statement)
        {
            Statement = statement;
        }
    }
}