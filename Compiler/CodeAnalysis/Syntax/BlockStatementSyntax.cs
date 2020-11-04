using System.Collections.Immutable;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class BlockStatementSyntax : StatementSyntax
    {
        public SyntaxToken OpenBraceToken { get; }
        public ImmutableArray<StatementSyntax> Statements { get; }
        public SyntaxToken CloseBraceToken { get; }
        public override SyntaxKind Kind => SyntaxKind.BlockStatement;

        public BlockStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken openBraceToken, 
            ImmutableArray<StatementSyntax> statements, 
            SyntaxToken closeBraceToken)
            : base(syntaxTree)
        {
            OpenBraceToken = openBraceToken;
            Statements = statements;
            CloseBraceToken = closeBraceToken;
        }
    }
}