namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class ParameterSyntax : SyntaxNode
    {
        public SyntaxToken Identifier { get; }
        public TypeClauseSyntax Type { get; }
        public override SyntaxKind Kind => SyntaxKind.Parameter;

        public ParameterSyntax(SyntaxToken identifier, TypeClauseSyntax type)
        {
            Identifier = identifier;
            Type = type;
        }
    }
}