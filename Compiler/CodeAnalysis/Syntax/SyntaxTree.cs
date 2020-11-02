using System.Collections.Immutable;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        public SourceText Text { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }

        private SyntaxTree(SourceText text)
        {
            Text = text;
            var parser = new Parser(text);
            Root = parser.ParseCompilationUnit();
            Diagnostics = parser.Diagnostics.ToImmutableArray();
        }

        public static SyntaxTree Parse(string text)
        {
            var sourceText = SourceText.From(text);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text)
        {
            return ParseTokens(text, out _);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics)
        {
            var sourceText = SourceText.From(text);
            return ParseTokens(sourceText, out diagnostics);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text)
        {
            return ParseTokens(text, out _);
        }
        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics)
        {
            var lexer = new Lexer(text);
            var builder = ImmutableArray.CreateBuilder<SyntaxToken>();
            
            while (true)
            {
                var token = lexer.Lex();
                if (token.Kind == SyntaxKind.EndOfFileToken)
                {
                    break;
                }
                builder.Add(token);
            }

            diagnostics = lexer.Diagnostics.ToImmutableArray();
            return builder.ToImmutable();
        }
    }
}