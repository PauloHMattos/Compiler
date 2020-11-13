using System;
using System.Collections.Generic;
using Compiler.CodeAnalysis.Authoring;
using Compiler.CodeAnalysis.Syntax;
using Xunit;
using Compiler.CodeAnalysis.Text;

namespace Compiler.Tests.CodeAnalysis.Authoring
{
    public class ClassifierTests
    {
        [Theory]
        [InlineData("1", Classification.Number)]
        [InlineData("123", Classification.Number)]
        [InlineData("\"123\"", Classification.String)]
        [InlineData("a", Classification.Identifier)]
        [InlineData("abc", Classification.Identifier)]
        [InlineData("//abc", Classification.Comment)]
        [InlineData("/* abc */", Classification.Comment)]
        [InlineData("true", Classification.Keyword)]
        [InlineData("var", Classification.Keyword)]
        [InlineData("function", Classification.Keyword)]
        [InlineData("+", Classification.Text)]
        [InlineData("\r\n", Classification.Whitespace)]
        [InlineData("\n", Classification.Whitespace)]
        [InlineData("\t", Classification.Whitespace)]
        [InlineData(" ", Classification.Whitespace)]
        public void Classifier_Classify_CorretResult(string text, Classification expectedClassification)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var classifiedSpans = Classifier.Classify(syntaxTree, syntaxTree.Root.FullSpan);
            var classifiedSpan = Assert.Single(classifiedSpans);
            Assert.Equal(expectedClassification, classifiedSpan.Classification);
            Assert.Equal(new TextSpan(0, text.Length), classifiedSpan.Span);
        }

        [Fact]
        public void Classifier_Classify_FullStatement()
        {
            var text = @"
                [// single line comment][
                ][/*
                multi line comment
                */][var][ ][a][=][10][// trailing comment]
            ";

            var classifications = new List<Classification>()
            {
                Classification.Comment,
                Classification.Whitespace,
                Classification.Comment,
                Classification.Keyword,
                Classification.Whitespace,
                Classification.Identifier,
                Classification.Text,
                Classification.Number,
                Classification.Comment,
            };

            AssertClassifiedSpan(text, classifications);
        }

        private void AssertClassifiedSpan(string text, List<Classification> expectedClassifications)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            var classifiedSpans = Classifier.Classify(syntaxTree, syntaxTree.Root.FullSpan);

            if (annotatedText.Spans.Length != classifiedSpans.Length)
            {
                throw new InvalidOperationException($"ERROR: Must mark the same number os spans as there are expected classifications {annotatedText.Spans.Length} vs {classifiedSpans.Length}");
            }

            Assert.Equal(expectedClassifications.Count, classifiedSpans.Length);
            for (var i = 0; i < classifiedSpans.Length; i++)
            {
                Assert.Equal(annotatedText.Spans[i], classifiedSpans[i].Span);
                Assert.Equal(expectedClassifications[i], classifiedSpans[i].Classification);
            }
        }
    }
}