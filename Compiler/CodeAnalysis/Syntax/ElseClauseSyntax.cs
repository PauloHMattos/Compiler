namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class ElseClauseSyntax : SyntaxNode
    {
        public SyntaxToken ElseKeyword { get; }
        public StatementSyntax ElseStatement { get; }
        public override SyntaxKind Kind => SyntaxKind.ElseClause;

        internal ElseClauseSyntax(SyntaxTree syntaxTree, 
            SyntaxToken elseKeyword, 
            StatementSyntax elseStatement)
            : base(syntaxTree)
        {
            ElseKeyword = elseKeyword;
            ElseStatement = elseStatement;
        }
    }
}