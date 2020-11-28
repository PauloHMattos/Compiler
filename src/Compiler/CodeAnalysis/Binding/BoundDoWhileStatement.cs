using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundDoWhileStatement : BoundLoopStatement
    {
        public BoundExpression Condition { get; }
        public BoundStatement Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStatement;

        public BoundDoWhileStatement(SyntaxNode syntax,
                                     BoundExpression condition,
                                     BoundStatement body,
                                     BoundLabel breakLabel,
                                     BoundLabel continueLabel)
            : base(syntax, breakLabel, continueLabel)
        {
            Condition = condition;
            Body = body;
        }
    }
}