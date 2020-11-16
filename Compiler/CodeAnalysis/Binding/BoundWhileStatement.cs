using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundWhileStatement : BoundLoopStatement
    {
        public BoundExpression Condition { get; }
        public BoundStatement Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;

        public BoundWhileStatement(SyntaxNode syntax,
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