using Compiler.CodeAnalysis.Syntax.Attributes;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class MemberAccessExpressionSyntax : NameExpressionSyntax
    {
        public ExpressionSyntax ParentExpression { get; }
        public SyntaxToken OperatorToken { get; }
        [DiscardFromChildren]
        public NameExpressionSyntax MemberExpression { get; }
        public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;

        internal MemberAccessExpressionSyntax(SyntaxTree syntaxTree,
                                            ExpressionSyntax parentExpression, 
                                            SyntaxToken operatorToken,
                                            NameExpressionSyntax memberExpression)
            : base(syntaxTree, memberExpression.IdentifierToken)
        {
            ParentExpression = parentExpression;
            OperatorToken = operatorToken;
            MemberExpression = memberExpression;
        }
    }
}