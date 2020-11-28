using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundReturnStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
        public BoundExpression? Expression { get; }
        
        public BoundReturnStatement(SyntaxNode syntax, BoundExpression? expression)
            : base(syntax)
        {
            Expression = expression;
        }

    }
}