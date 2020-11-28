namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class BinaryExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Left { get; }
        public SyntaxToken OperatorToken { get; }
        public ExpressionSyntax Right { get; }
        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

        internal BinaryExpressionSyntax(SyntaxTree syntaxTree, 
            ExpressionSyntax left, 
            SyntaxToken operatorToken, 
            ExpressionSyntax right)
            : base(syntaxTree)
        {
            Left = left;
            OperatorToken = operatorToken;
            Right = right;
        }
    }
}