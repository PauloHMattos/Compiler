namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax ParentExpression { get; }
        public SyntaxToken OperatorToken { get; }
        public NameExpressionSyntax MemberExpression { get; }
        public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;

        internal MemberAccessExpressionSyntax(SyntaxTree syntaxTree,
                                            ExpressionSyntax parentExpression, 
                                            SyntaxToken operatorToken,
                                            NameExpressionSyntax memberExpression)
            : base(syntaxTree)
        {
            ParentExpression = parentExpression;
            OperatorToken = operatorToken;
            MemberExpression = memberExpression;
        }
    }
}