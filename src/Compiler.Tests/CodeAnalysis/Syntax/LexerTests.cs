using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;
using Xunit;

namespace Compiler.Tests.CodeAnalysis.Syntax
{
    public class LexerTests
    {
        [Fact]
        public void Lexer_Lexes_UnterminateString()
        {
            const string text = "\"text";
            var tokens = SyntaxTree.ParseTokens(text, out var diagnostics);

            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.StringToken, token.Kind);
            Assert.Equal(text, token.Text);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(new TextSpan(0, 1), diagnostic.Location.Span);
            Assert.Equal(DiagnosticCode.UnterminatedString.GetDiagnostic(), diagnostic.Message);
        }

        [Fact]
        public void Lexer_Cover_AllTokenKinds()
        {
            var tokenKinds = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(k => k.IsToken())
                .ToList();


            var testedTokenKinds = GetTokens()
                .Concat(GetSeparators())
                .Select(t => t.kind);

            var untestedTokenKinds = new SortedSet<SyntaxKind>(tokenKinds);
            untestedTokenKinds.ExceptWith(testedTokenKinds);
            untestedTokenKinds.Remove(SyntaxKind.BadToken);
            untestedTokenKinds.Remove(SyntaxKind.EndOfFileToken);

            Assert.Empty(untestedTokenKinds);
        }

        [Theory]
        [MemberData(nameof(GetTokensData))]
        public void Lexer_Lexes_Token(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text);
            var token = Assert.Single(tokens);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsData))]
        public void Lexer_Lexes_TokenPairs(SyntaxKind kind1, string text1,
            SyntaxKind kind2, string text2)
        {
            var text = text1 + text2;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);

            Assert.Equal(kind1, tokens[0].Kind);
            Assert.Equal(text1, tokens[0].Text);

            Assert.Equal(kind2, tokens[1].Kind);
            Assert.Equal(text2, tokens[1].Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsWithSeparatorData))]
        public void Lexer_Lexes_TokenPairsWithSeparators(SyntaxKind kind1, string text1,
                                                         SyntaxKind separatorKind, string separatorText,
                                                         SyntaxKind kind2, string text2)
        {
            var text = text1 + separatorText + text2;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);

            Assert.Equal(kind1, tokens[0].Kind);
            Assert.Equal(text1, tokens[0].Text);

            var separator = Assert.Single(tokens[0].TrailingTrivia);
            Assert.Equal(separatorKind, separator.Kind);
            Assert.Equal(separatorText, separator.Text);

            Assert.Equal(kind2, tokens[1].Kind);
            Assert.Equal(text2, tokens[1].Text);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("foo42")]
        [InlineData("foo_42")]
        [InlineData("_foo")]
        public void Lexer_Lexes_Identifiers(string name)
        {
            var tokens = SyntaxTree.ParseTokens(name).ToArray();

            Assert.Single(tokens);

            var token = tokens[0];
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
            Assert.Equal(name, token.Text);
        }

        [Theory]
        [MemberData(nameof(GetSeparatorsData))]
        public void Lexer_Lexes_Separator(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text, includeEndOfFile: true);

            var token = Assert.Single(tokens);
            var trivia = Assert.Single(token.LeadingTrivia);
            Assert.Equal(kind, trivia.Kind);
            Assert.Equal(text, trivia.Text);
        }

        public static IEnumerable<object[]> GetTokensData()
        {
            foreach (var (kind, text) in GetTokens())
            {
                yield return new object[] { kind, text };
            }
        }

        public static IEnumerable<object[]> GetSeparatorsData()
        {
            foreach (var (kind, text) in GetSeparators())
            {
                yield return new object[] { kind, text };
            }
        }

        public static IEnumerable<object[]> GetTokenPairsData()
        {
            foreach (var (kind1, text1, kind2, text2) in GetTokenPairs())
            {
                yield return new object[] { kind1, text1, kind2, text2 };
            }
        }

        public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
        {
            foreach (var (kind1, text1, separatorKind, separatorText, kind2, text2) in GetTokenPairsWithSeparator())
            {
                yield return new object[] { kind1, text1, separatorKind, separatorText, kind2, text2 };
            }
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
        {
            var fixedTokens = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Select(k => (kind: k, text: k.GetText()))
                .Where(t => t.text != null)
                .Cast<(SyntaxKind, string)>();

            var dynamicTokens = new[]
            {
                (SyntaxKind.IdentifierToken, "a"),
                (SyntaxKind.IdentifierToken, "abc"),
                (SyntaxKind.NumberToken, "1"),
                (SyntaxKind.NumberToken, "123"),
                (SyntaxKind.StringToken, "\"Test\""),
                (SyntaxKind.StringToken, "\"Te\"\"st\""),
            };

            return fixedTokens.Concat(dynamicTokens);
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
        {
            return new[]
            {
                (SyntaxKind.WhitespaceTrivia, " "),
                (SyntaxKind.WhitespaceTrivia, "  "),
                (SyntaxKind.WhitespaceTrivia, "\t"),
                (SyntaxKind.LineBreakTrivia, "\r"),
                (SyntaxKind.LineBreakTrivia, "\n"),
                (SyntaxKind.LineBreakTrivia, "\r\n"),
                (SyntaxKind.MultiLineCommentTrivia, "/**/"),
            };
        }

        private static bool RequiresSeparator(SyntaxKind kind1, SyntaxKind kind2)
        {
            var kind1IsKeyword = kind1.IsKeyword();
            var kind2IsKeyword = kind2.IsKeyword();

            if (kind1 == SyntaxKind.IdentifierToken && kind2 == SyntaxKind.IdentifierToken)
                return true;

            if (kind1IsKeyword && kind2IsKeyword)
                return true;

            if (kind1IsKeyword && kind2 == SyntaxKind.IdentifierToken)
                return true;

            if (kind1 == SyntaxKind.IdentifierToken && kind2 == SyntaxKind.NumberToken)
                return true;

            if (kind1IsKeyword && kind2 == SyntaxKind.NumberToken)
                return true;

            if (kind1 == SyntaxKind.IdentifierToken && kind2IsKeyword)
                return true;

            if (kind1 == SyntaxKind.NumberToken && kind2 == SyntaxKind.NumberToken)
                return true;

            if (kind1 == SyntaxKind.BangToken && kind2 == SyntaxKind.EqualsToken)
                return true;
            
            if (kind1 == SyntaxKind.BangToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.EqualsToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.EqualsToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;
            
            if (kind1 == SyntaxKind.LessToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.LessToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.GreaterToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.GreaterToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.AmpersandToken && kind2 == SyntaxKind.AmpersandToken)
                return true;

            if (kind1 == SyntaxKind.AmpersandToken && kind2 == SyntaxKind.AmpersandAmpersandToken)
                return true;

            if (kind1 == SyntaxKind.PipeToken && kind2 == SyntaxKind.PipeToken)
                return true;

            if (kind1 == SyntaxKind.PipeToken && kind2 == SyntaxKind.PipePipeToken)
                return true;

            if (kind1 == SyntaxKind.StringToken && kind2 == SyntaxKind.StringToken)
                return true;

            
            if (kind1 == SyntaxKind.PlusToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.PlusToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.MinusToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.MinusToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.StarToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.StarToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.AmpersandToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.AmpersandToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.AmpersandToken && kind2 == SyntaxKind.AmpersandEqualsToken)
                return true;

            if (kind1 == SyntaxKind.PipeToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.PipeToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.PipeToken && kind2 == SyntaxKind.PipeEqualsToken)
                return true;

            if (kind1 == SyntaxKind.HatToken && kind2 == SyntaxKind.EqualsToken)
                return true;

            if (kind1 == SyntaxKind.HatToken && kind2 == SyntaxKind.EqualsEqualsToken)
                return true;

            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.SlashEqualsToken)
                return true;

            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.StarEqualsToken)
                return true;

            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.SlashToken)
                return true;

            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.StarToken)
                return true;
            
            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.SingleLineCommentTrivia)
                return true;

            if (kind1 == SyntaxKind.SlashToken && kind2 == SyntaxKind.MultiLineCommentTrivia)
                return true;

            return false;
        }

        public static IEnumerable<(SyntaxKind kind1, string text1, SyntaxKind kind2, string text2)> GetTokenPairs()
        {
            foreach (var (kind1, text1) in GetTokens())
            {
                foreach (var (kind2, text2) in GetTokens())
                {
                    if (RequiresSeparator(kind1, kind2))
                    {
                        continue;
                    }

                    yield return (kind1, text1, kind2, text2);
                }
            }
        }

        public static IEnumerable<(SyntaxKind kind1, string text1, 
                                    SyntaxKind separatorKind, string separatorText,
                                    SyntaxKind kind2, string text2)> 
                            GetTokenPairsWithSeparator()
        {
            foreach (var (kind1, text1) in GetTokens())
            {
                foreach (var (kind2, text2) in GetTokens())
                {
                    if (!RequiresSeparator(kind1, kind2))
                    {
                        continue;
                    }

                    foreach (var (separatorKind, separatorText) in GetSeparators())
                    {
                        if (RequiresSeparator(kind1, separatorKind) || 
                            RequiresSeparator(kind2, separatorKind))
                        {
                            continue;
                        }
                        yield return (kind1, text1, separatorKind, separatorText, kind2, text2);
                    }
                }
            }
        }
    }
}
