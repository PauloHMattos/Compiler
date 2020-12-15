namespace Compiler.CodeAnalysis.Binding.Scopes
{
    internal sealed class BlockBoundScope : BoundScope
    {
        public BlockBoundScope(IBoundScope parent)
            : base(parent, parent.Diagnostics)
        {
        }
    }
}