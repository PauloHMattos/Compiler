using System.Collections.Generic;
using Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Compiler.Tests.CodeAnalysis.Syntax
{
    public class ParserTests
    {
        [Theory]
        [MemberData(nameof(GetBinaryOperatorsPairsData))]
        public void Parser_BinaryExpression_HonorsPrecedences(SyntaxKind op1, SyntaxKind op2)
        {
            var op1Precedence = op1.GetBinaryOperatorPrecedence();
            var op2Precedence = op2.GetBinaryOperatorPrecedence();
            var op1Text = op1.GetText();
            var op2Text = op2.GetText();

            var text = $"a {op1Text} b {op2Text} c";
            var expression = ParseExpression(text);

            if (op1Precedence >= op2Precedence)
            {
                //       op2
                //      /  \
                //    op1   c
                //   /  \
                //  a    b
                using (var e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(op1, op1Text);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                    e.AssertToken(op2, op2Text);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "c");
                }
            }
            else
            {
                //    op1
                //   /  \
                //  a   op2
                //     /  \
                //    b    c
                using (var e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(op1, op1Text);
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                    e.AssertToken(op2, op2Text);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "c");
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetUnaryOperatorsPairsData))]
        public void Parser_UnaryExpression_HonorsPrecedences(SyntaxKind unaryKind, SyntaxKind binaryKind)
        {
            var unaryPrecedence = unaryKind.GetUnaryOperatorPrecedence();
            var binaryPrecedence = binaryKind.GetBinaryOperatorPrecedence();
            var unaryText = unaryKind.GetText();
            var binaryText = binaryKind.GetText();

            var text = $"{unaryText} a {binaryText} b";
            var expression = ParseExpression(text);

            if (unaryPrecedence >= binaryPrecedence)
            {
                //     binary
                //    /     \
                // unary     b
                //   |
                //   a
                using (var e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.UnaryExpression);
                    e.AssertToken(unaryKind, unaryText);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(binaryKind, binaryText);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                }
            }
            else
            {
                //   unary
                //     |
                //   binary
                //   /  \
                //  a    b
                using (var e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.UnaryExpression);
                    e.AssertToken(unaryKind, unaryText);
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(binaryKind, binaryText);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                }
            }
        }
        
        private static ExpressionSyntax ParseExpression(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var root = syntaxTree.Root;
            var statement = root.Statement;
            return Assert.IsType<ExpressionStatementSyntax>(statement).Expression;
        }

        public static IEnumerable<object[]> GetBinaryOperatorsPairsData()
        {
            foreach (var op1 in SyntaxFacts.GetBinaryOperatorKinds())
            {
                foreach (var op2 in SyntaxFacts.GetBinaryOperatorKinds())
                {
                    yield return new object[] { op1, op2 };
                }
            }
        }

        public static IEnumerable<object[]> GetUnaryOperatorsPairsData()
        {
            foreach (var unary in SyntaxFacts.GetUnaryOperatorKinds())
            {
                foreach (var binary in SyntaxFacts.GetBinaryOperatorKinds())
                {
                    yield return new object[] { unary, binary };
                }
            }
        }
    }
}