using Compiler.CodeAnalysis.Text;
using Xunit;

namespace Compiler.Tests.CodeAnalysis.Text
{
    public class TextSpanTest
    {
        [Fact]
        public void TextSpan_Constructor()
        {
            var span = new TextSpan(0, 42);
            Assert.Equal(0, span.Start);
            Assert.Equal(42, span.Length);
            Assert.Equal(42, span.End);
        }

        [Fact]
        public void TextSpan_Constructor_ZeroLenghtSpan()
        {
            var span = new TextSpan(0, 0);
            Assert.Equal(0, span.Start);
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public void TextSpan_End_Correct()
        {
            var span = new TextSpan(10, 10);
            Assert.Equal(20, span.End);
            Assert.Equal(span.Start + span.Length, span.End);
        }

        [Fact]
        public void TextSpan_Equals()
        {
            var span1 = new TextSpan(1, 40);
            var span2 = new TextSpan(1, 40);
            var message = $"{span1} : {span2}";
            Assert.True(span1.Equals(span2), message);
            Assert.Equal(span1, span2);
        }

        [Fact]
        public void TextSpan_ToString()
        {
            var span = new TextSpan(0, 1);
            Assert.Equal("[0..1)", span.ToString());
        }

        [Fact]
        public void TextSpan_FromBounds()
        {
            var span1 = new TextSpan(10, 10);
            var span2 = TextSpan.FromBounds(10, 20);
            Assert.Equal(span1, span2);
        }

        [Fact]
        public void TextSpan_OverlapWith_Begin()
        {
            // Case 1
            //          [---------------------]
            // [------------]
            var span1 = TextSpan.FromBounds(10, 20);
            var span2 = TextSpan.FromBounds(5, 15);

            Assert.True(span1.OverlapsWith(span2));
            Assert.True(span2.OverlapsWith(span1));
        }

        [Fact]
        public void TextSpan_OverlapWith_End()
        {
            // Case 2
            //          [---------------------]
            //                          [------------]
            var span1 = TextSpan.FromBounds(5, 15);
            var span2 = TextSpan.FromBounds(10, 20);

            Assert.True(span1.OverlapsWith(span2));
            Assert.True(span2.OverlapsWith(span1));
        }
        
        [Fact]
        public void TextSpan_OverlapWith_Contains()
        {
            // Case 3
            //          [---------------------]
            //              [------------]
            var span1 = TextSpan.FromBounds(5, 25);
            var span2 = TextSpan.FromBounds(10, 20);

            Assert.True(span1.OverlapsWith(span2));
            Assert.True(span2.OverlapsWith(span1));
        }
        
        [Fact]
        public void TextSpan_OverlapWith_NoOverlap()
        {
            // Case 5
            // [----------]
            //               [------------]
            var span1 = TextSpan.FromBounds(0, 5);
            var span2 = TextSpan.FromBounds(5, 10);

            Assert.False(span1.OverlapsWith(span2));
            Assert.False(span2.OverlapsWith(span1));
        }
    }
}