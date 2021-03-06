﻿namespace Compiler.CodeAnalysis.Syntax
{
    internal partial class ContinueStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; }
        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;

        internal ContinueStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken keyword)
            : base(syntaxTree)
        {
            Keyword = keyword;
        }
    }
}