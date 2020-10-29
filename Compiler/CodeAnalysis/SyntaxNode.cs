using System.Collections.Generic;

namespace Compiler.CodeAnalysis
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }
        
        public abstract IEnumerable<SyntaxNode> GetChildren();
    }
}