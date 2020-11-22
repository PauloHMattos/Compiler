using System.Collections.Immutable;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class MemberBlockStatementSyntax : MemberSyntax
    {
        public SyntaxToken OpenBrace { get; }
        public ImmutableArray<StatementSyntax> Statement { get; }
        public SyntaxToken CloseBrace { get; }
        public override SyntaxKind Kind => SyntaxKind.MemberBlockStatement;

        internal MemberBlockStatementSyntax(SyntaxTree syntaxTree, SyntaxToken openBrace, ImmutableArray<StatementSyntax> statements, SyntaxToken closeBrace)
            : base(syntaxTree)
        {
            OpenBrace = openBrace;
            Statement = statements;
            CloseBrace = closeBrace;
        }
    }
}