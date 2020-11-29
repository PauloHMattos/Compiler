using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundCompoundMemberAssignmentExpression : BoundExpression
    {
        public BoundExpression Instance { get; }
        public MemberSymbol Member { get; }
        public BoundBinaryOperator Operator {get; }
        public BoundExpression Expression { get; }

        public override TypeSymbol Type => Expression.Type;
        public override BoundNodeKind Kind => BoundNodeKind.CompoundMemberAssignmentExpression;
        
        public BoundCompoundMemberAssignmentExpression(SyntaxNode syntax, BoundExpression structInstance, MemberSymbol member, BoundBinaryOperator op, BoundExpression expression)
            : base(syntax)
        {
            Instance = structInstance;
            Member = member;
            Operator = op;
            Expression = expression;
        }
    }
}