using System.Collections.Generic;
using Compiler.CodeAnalysis;
using Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Compiler.Tests.CodeAnalysis
{
    public class EvaluatorTests
    {
        [Theory]
        [InlineData("1", 1)]
        [InlineData("+1", 1)]
        [InlineData("-1", -1)]
        [InlineData("+-1", -1)]
        [InlineData("1+2", 3)]
        [InlineData("1-2", -1)]
        [InlineData("1*2", 2)]
        [InlineData("100/10", 10)]
        [InlineData("(1 + 2)", 3)]
        [InlineData("1 == 2", false)]
        [InlineData("1 != 2", true)]
        [InlineData("1+1 == 2", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("true == true", true)]
        [InlineData("true || true", true)]
        [InlineData("true && true", true)]
        [InlineData("true == false", false)]
        [InlineData("true || false", true)]
        [InlineData("true && false", false)]
        [InlineData("false == false", true)]
        [InlineData("a = 1", 1)]
        [InlineData("a = true", true)]
        [InlineData("(a = 20) * 10", 200)]
        public void Evaluator_Evaluate_RoundTrips(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = new Compilation(syntaxTree);

            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }
    }
}
