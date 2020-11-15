using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxTrivia
    {
        public SyntaxTree SyntaxTree { get; }
        public SyntaxKind Kind { get; }
        public TextSpan Span { get; }
        public string Text { get; }

        public SyntaxTrivia(SyntaxTree syntaxTree, SyntaxKind kind, int position, string text)
        {
            SyntaxTree = syntaxTree;
            Kind = kind;
            Text = text;
            Span = new TextSpan(position, Text.Length);
        }
    }
}