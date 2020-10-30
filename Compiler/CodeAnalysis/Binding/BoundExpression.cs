using System;

namespace Compiler.CodeAnalysis.Binding
{
    public abstract class BoundExpression : BoundNode
    {
        public abstract Type Type { get; }
    }
}