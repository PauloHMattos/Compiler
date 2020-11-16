namespace Compiler.CodeAnalysis.Syntax
{
    public sealed partial class ParameterSyntax : SyntaxNode
    {
        public SyntaxToken Identifier { get; }
        public TypeClauseSyntax Type { get; }
        public override SyntaxKind Kind => SyntaxKind.Parameter;

        internal ParameterSyntax(SyntaxTree syntaxTree, 
            SyntaxToken identifier, 
            TypeClauseSyntax type)
            : base(syntaxTree)
        {
            Identifier = identifier;
            Type = type;
        }
    }
}