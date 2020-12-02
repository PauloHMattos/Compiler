using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundAssignmentExpression : BoundExpression
    {
        public BoundExpression Left { get; }
        public BoundExpression Right { get; }
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
        public override TypeSymbol Type => Left.Type;

        public BoundAssignmentExpression(SyntaxNode syntax,
                                         BoundExpression left,
                                         BoundExpression right)
            : base(syntax)
        {
            Left = left;
            Right = right;
        }
    }
}