using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundIfStatement : BoundStatement
    {
        public BoundExpression Condition { get; }
        public BoundStatement ThenStatement { get; }
        public BoundStatement? ElseStatement { get; }
        public override BoundNodeKind Kind => BoundNodeKind.IfStatement;

        public BoundIfStatement(SyntaxNode syntax,
                                BoundExpression condition,
                                BoundStatement thenStatement,
                                BoundStatement? elseStatement)
            : base(syntax)
        {
            Condition = condition;
            ThenStatement = thenStatement;
            ElseStatement = elseStatement;
        }
    }
}