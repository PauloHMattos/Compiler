using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundMemberExpression : BoundExpression
    {
        public MemberSymbol Symbol { get; }
        public override TypeSymbol Type => Symbol.Type;
        public MemberKind MemberKind => Symbol.MemberKind;
        public override BoundNodeKind Kind => BoundNodeKind.MemberExpression;

        public BoundMemberExpression(SyntaxNode syntax, MemberSymbol symbol)
            : base(syntax)
        {
            Symbol = symbol;
        }
    }
}