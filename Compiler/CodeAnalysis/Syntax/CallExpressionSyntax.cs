using System.Collections.Generic;

namespace Compiler.CodeAnalysis.Syntax
{
    internal class CallExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenParenthesisToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenthesisToken { get; }
        public override SyntaxKind Kind => SyntaxKind.CallExpression;

        public CallExpressionSyntax(SyntaxTree syntaxTree, 
            SyntaxToken identifier, 
            SyntaxToken openParenthesisToken, 
            SeparatedSyntaxList<ExpressionSyntax> arguments, 
            SyntaxToken closeParenthesisToken)
            : base(syntaxTree)
        {
            Identifier = identifier;
            OpenParenthesisToken = openParenthesisToken;
            Arguments = arguments;
            CloseParenthesisToken = closeParenthesisToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return OpenParenthesisToken;
            foreach (var arg in Arguments.GetWithSeparators())
            {
                yield return arg;
            }
            yield return CloseParenthesisToken;
        }
    }
}