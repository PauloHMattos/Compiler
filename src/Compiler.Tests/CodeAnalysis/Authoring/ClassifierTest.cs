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
        [InlineData("//abc", Classification.Comment)]
        [InlineData("/* abc */", Classification.Comment)]
        [InlineData("function", Classification.Keyword)]
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
                [// single line comment]
                function main()
                {
                    [/*
                    multi line comment
                    */]
                    [var] [a] [=] [10] [// trailing comment]
                }
            ";

            var classifications = new List<Classification>()
            {
                Classification.Comment,
                Classification.Comment,
                Classification.Keyword,
                Classification.Identifier,
                Classification.Text,
                Classification.Number,
                Classification.Comment,
            };

            AssertClassifiedSpan(text, classifications);
        }

        [Fact]
        public void Classifier_Classify_OptionalElseStatement()
        {
            var text = @"
                function main()
                {
                    [if] a call1()
                    [else] [call2][(][)]
                }
            ";

            var classifications = new List<Classification>()
            {
                Classification.Keyword,
                Classification.Keyword,
                Classification.Identifier,
                Classification.Text,
                Classification.Text,
            };

            AssertClassifiedSpan(text, classifications);
        }

        [Fact]
        public void Classifier_Classify_OptionalStepStatement()
        {
            var text = @"
                function main()
                {
                    [for] i = 0 [to] 100 [step] [2]
                    [{]
                        
                    [}]
                }
            ";

            var classifications = new List<Classification>()
            {
                Classification.Keyword,
                Classification.Keyword,
                Classification.Keyword,
                Classification.Number,
                Classification.Text,
                Classification.Text,
            };

            AssertClassifiedSpan(text, classifications);
        }

        private static void AssertClassifiedSpan(string text, List<Classification> expectedClassifications)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);

            if (annotatedText.Spans.Length != expectedClassifications.Count)
            {
                throw new InvalidOperationException("ERROR: Must mark the same number os spans as there are expected classifications");
            }

            for (var i = 0; i < expectedClassifications.Count; i++)
            {
                var classifiedSpans = Classifier.Classify(syntaxTree, annotatedText.Spans[i]);
                var classifiedSpan = Assert.Single(classifiedSpans);
                Assert.Equal(annotatedText.Spans[i], classifiedSpan.Span);
                Assert.Equal(expectedClassifications[i], classifiedSpan.Classification);
            }
        }
    }
}