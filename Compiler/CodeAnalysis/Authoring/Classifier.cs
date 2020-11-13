using System.Collections.Immutable;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Authoring
{
    public static class Classifier
    {
        public static ImmutableArray<ClassifiedSpan> Classify(SyntaxTree syntaxTree, TextSpan span)
        {
            var builder = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            ClassifyNode(syntaxTree.Root, span, builder);
            return builder.ToImmutable();
        }

        private static void ClassifyNode(SyntaxNode node, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder builder)
        {
            if (node == null || !node.FullSpan.OverlapsWith(span))
            {
                return;
            }

            if (node is SyntaxToken token)
            {
                ClassifyToken(token, span, builder);
            }

            foreach (var child in node.GetChildren())
            {
                ClassifyNode(child, span, builder);
            }
        }

        private static void ClassifyToken(SyntaxToken token, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder builder)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                AddClassification(trivia.Kind, trivia.Span, span, builder);
            }

            AddClassification(token.Kind, token.Span, span, builder);

            foreach (var trivia in token.TrailingTrivia)
            {
                AddClassification(trivia.Kind, trivia.Span, span, builder);
            }
        }

        private static void AddClassification(SyntaxKind elementKind,
                                              TextSpan elementSpan,
                                              TextSpan span,
                                              ImmutableArray<ClassifiedSpan>.Builder builder)
        {
            var overlap = elementSpan.Overlap(span);
            if (!overlap.HasValue)
                return;

            var classification = GetClassification(elementKind);
            var classifiedSpan = new ClassifiedSpan(overlap.Value, classification);
            builder.Add(classifiedSpan);
        }

        private static Classification GetClassification(SyntaxKind kind)
        {
            if (kind.IsKeyword())
            {
                return Classification.Keyword;
            }
            else if(kind == SyntaxKind.IdentifierToken)
            {
                return Classification.Identifier;
            }
            else if(kind == SyntaxKind.NumberToken)
            {
                return Classification.Number;
            }
            else if(kind == SyntaxKind.StringToken)
            {
                return Classification.String;
            }
            else if(kind.IsComment())
            {
                return Classification.Comment;
            }
            else if(kind.IsWhitespace())
            {
                return Classification.Whitespace;
            }
            return Classification.Text;
        }
    }
}