﻿namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class IfStatementSyntax : StatementSyntax
    {
        public SyntaxToken IfKeyword { get; }
        public ExpressionSyntax Condition { get; }
        public StatementSyntax ThenStatement { get; }
        public ElseClauseSyntax ElseClause { get; }
        public override SyntaxKind Kind => SyntaxKind.IfStatement;

        public IfStatementSyntax(SyntaxTree syntaxTree, 
            SyntaxToken ifKeyword, 
            ExpressionSyntax condition, 
            StatementSyntax thenStatement, 
            ElseClauseSyntax elseClause)
            : base(syntaxTree)
        {
            IfKeyword = ifKeyword;
            Condition = condition;
            ThenStatement = thenStatement;
            ElseClause = elseClause;
        }
    }
}