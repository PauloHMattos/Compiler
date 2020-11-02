using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public int Position { get; }
        public string Text { get; }
        public object Value { get; }
        public override SyntaxKind Kind { get; }
        public override TextSpan Span { get; }
        public bool IsMissing => Span.Length == 0;

        public SyntaxToken(SyntaxKind kind, int position, string text, object value)
        {
            Kind = kind;
            Position = position;
            Text = text;
            Value = value;
            Span = new TextSpan(position, Text?.Length ?? 0);
        }
    }
}