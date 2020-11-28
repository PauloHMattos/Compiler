using Compiler.CodeAnalysis.Text;
using Xunit;

namespace Compiler.Tests.CodeAnalysis.Text
{
    public class SourceTextTest
    {
        [Theory]
        [InlineData(".", 1)]
        [InlineData("\n\r", 2)]
        [InlineData("\n\r\r\n", 3)]
        public void SourceText_IncludesLastLine(string text, int expectedLineCount)
        {
            var sourceText = SourceText.From(text);
            Assert.Equal(expectedLineCount, sourceText.Lines.Length);
        }
    }
}