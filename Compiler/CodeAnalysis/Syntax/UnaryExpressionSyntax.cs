namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class UnaryExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OperatorToken { get; }
        public ExpressionSyntax Operand { get; }
        public override SyntaxKind Kind => SyntaxKind.UnaryExpression;

        public UnaryExpressionSyntax(SyntaxTree syntaxTree, 
            SyntaxToken operatorToken, 
            ExpressionSyntax operand)
            : base(syntaxTree)
        {
            OperatorToken = operatorToken;
            Operand = operand;
        }
    }
}