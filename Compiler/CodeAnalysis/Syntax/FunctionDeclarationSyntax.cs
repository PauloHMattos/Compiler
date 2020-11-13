using System.Collections.Generic;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class FunctionDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken FunctionKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenParenthesisToken { get; }
        public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }
        public SyntaxToken CloseParenthesisToken { get; }
        public TypeClauseSyntax Type { get; }
        public BlockStatementSyntax Body { get; }
        public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;

        public FunctionDeclarationSyntax(SyntaxTree syntaxTree, 
            SyntaxToken functionKeyword, 
            SyntaxToken identifier, 
            SyntaxToken openParenthesisToken, 
            SeparatedSyntaxList<ParameterSyntax> parameters, 
            SyntaxToken closeParenthesisToken, 
            TypeClauseSyntax type,
            BlockStatementSyntax body)
            : base(syntaxTree)
        {
            FunctionKeyword = functionKeyword;
            Identifier = identifier;
            OpenParenthesisToken = openParenthesisToken;
            Parameters = parameters;
            CloseParenthesisToken = closeParenthesisToken;
            Type = type;
            Body = body;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return FunctionKeyword;
            yield return Identifier;
            yield return OpenParenthesisToken;
            foreach (var arg in Parameters.GetWithSeparators())
            {
                yield return arg;
            }
            yield return CloseParenthesisToken;
            yield return Type;
            yield return Body;
        }
    }
}