using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundMemberAccessExpression : BoundExpression
    {
        public BoundExpression Instance { get; }
        public MemberSymbol Member { get; }
        public override TypeSymbol Type => Member.Type;
        public override BoundNodeKind Kind => BoundNodeKind.MemberAccessExpression;

        
        public BoundMemberAccessExpression(SyntaxNode syntax, 
                                          BoundExpression instance,
                                          MemberSymbol member)
            : base(syntax)
        {
            Instance = instance;
            Member = member;
        }
    }
}