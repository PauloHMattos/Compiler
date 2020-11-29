using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundMemberAssignmentExpression : BoundExpression
    {
        public BoundExpression Instance { get; }
        public MemberSymbol Member { get; }
        public BoundExpression Expression { get; }
        public override TypeSymbol Type => Expression.Type;
        public override BoundNodeKind Kind => BoundNodeKind.MemberAssignmentExpression;

        public BoundMemberAssignmentExpression(SyntaxNode syntax, BoundExpression instance, MemberSymbol member, BoundExpression expression)
            : base(syntax)
        {
            Instance = instance;
            Member = member;
            Expression = expression;
        }
    }
}