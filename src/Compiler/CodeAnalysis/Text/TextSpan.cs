using System;

namespace Compiler.CodeAnalysis.Text
{
    public readonly struct TextSpan
    {
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public static TextSpan FromBounds(in int start, in int end)
        {
            return new TextSpan(start, end - start);
        }

        public bool OverlapsWith(in TextSpan span)
        {
            return Start < span.End && End > span.Start;
        }

        public TextSpan? Overlap(TextSpan span)
        {
            int overlapStart = Math.Max(Start, span.Start);
            int overlapEnd = Math.Min(End, span.End);

            return overlapStart < overlapEnd
                ? FromBounds(overlapStart, overlapEnd)
                : (TextSpan?)null;
        }

        public override string ToString() => $"[{Start}..{End})";
    }
}
