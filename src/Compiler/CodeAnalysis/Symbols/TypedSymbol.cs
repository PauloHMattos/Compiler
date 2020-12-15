using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class TypedSymbol : Symbol
    {
        public TypeSymbol Type { get; }
        internal virtual BoundConstant? Constant { get; }

        private protected TypedSymbol(SyntaxNode? syntax,
                                      string name,
                                      TypeSymbol type,
                                      BoundConstant? constant)
            : base(syntax, name)
        {
            Type = type;
            Constant = constant;
        }
    }
}