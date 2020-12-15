namespace Compiler.CodeAnalysis.Syntax
{
    public abstract class MemberSyntax : StatementSyntax
    {
        private protected MemberSyntax(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }
    }
}