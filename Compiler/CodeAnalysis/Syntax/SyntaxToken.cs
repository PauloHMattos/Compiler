using System;
using System.Linq;
using Compiler.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public int Position { get; }
        public string Text { get; }
        public object? Value { get; }
        public override SyntaxKind Kind { get; }
        public override TextSpan Span => new TextSpan(Position, Text.Length);
        public override TextSpan FullSpan
        {
            get
            {
                var start = LeadingTrivia.Length == 0 ? Span.Start : LeadingTrivia.First().Span.Start;
                var end = TrailingTrivia.Length == 0 ? Span.End : TrailingTrivia.Last().Span.End;
                return TextSpan.FromBounds(start, end);
            }
        }
        public bool IsMissing { get; }
        public ImmutableArray<SyntaxTrivia> LeadingTrivia { get; }
        public ImmutableArray<SyntaxTrivia> TrailingTrivia { get; }

        public SyntaxToken(SyntaxTree syntaxTree,
            SyntaxKind kind,
            int position,
            string? text,
            object? value,
            ImmutableArray<SyntaxTrivia> leadingTrivia,
            ImmutableArray<SyntaxTrivia> trailingTrivia)
            : base(syntaxTree)
        {
            Kind = kind;
            Position = position;
            Text = text ?? string.Empty;
            Value = value;
            LeadingTrivia = leadingTrivia;
            TrailingTrivia = trailingTrivia;

            IsMissing = text == null;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Array.Empty<SyntaxNode>();
        }
    }
}