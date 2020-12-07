using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal abstract class BoundMemberExpression : BoundExpression
    {
        public MemberSymbol Symbol { get; }
        public override TypeSymbol Type => Symbol.Type;
        public MemberKind MemberKind => Symbol.MemberKind;

        protected BoundMemberExpression(SyntaxNode syntax, MemberSymbol symbol)
            : base(syntax)
        {
            Symbol = symbol;
        }
    }

    internal sealed class BoundFieldExpression : BoundMemberExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.FieldExpression;

        public BoundFieldExpression(SyntaxNode syntax, FieldSymbol symbol)
            : base(syntax, symbol)
        {
        }
    }

    internal sealed class BoundMemberAccessExpression : BoundExpression
    {
        public BoundExpression Instance { get; }
        public BoundMemberExpression Member { get; }
        public override TypeSymbol Type => Member.Type;
        public override BoundNodeKind Kind => BoundNodeKind.MemberAccessExpression;

        
        public BoundMemberAccessExpression(SyntaxNode syntax, 
                                          BoundExpression instance,
                                          BoundMemberExpression member)
            : base(syntax)
        {
            Instance = instance;
            Member = member;
        }
    }
}