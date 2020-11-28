using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Emit
{
    internal readonly struct Fixup
    {
        public int InstructionIndex { get; }
        public BoundLabel Target { get; }

        public Fixup(int index, BoundLabel target)
        {
            InstructionIndex = index;
            Target = target;
        }
    }
}