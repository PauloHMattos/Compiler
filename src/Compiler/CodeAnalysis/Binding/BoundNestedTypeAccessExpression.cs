using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundNestedTypeAccessExpression : BoundExpression
    {
        public BoundExpression Instance { get; }
        public BoundTypeReferenceExpression Member { get; }
        public override TypeSymbol Type => Member.Type;
        public override BoundNodeKind Kind => BoundNodeKind.NestedTypeAccessExpression;

        
        public BoundNestedTypeAccessExpression(SyntaxNode syntax,
                                               BoundExpression instance,
                                               BoundTypeReferenceExpression member)
            : base(syntax)
        {
            Instance = instance;
            Member = member;
        }
    }
}