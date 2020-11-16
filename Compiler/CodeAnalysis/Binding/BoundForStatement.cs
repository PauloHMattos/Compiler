using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundForStatement : BoundLoopStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression LowerBound { get; }
        public BoundExpression UpperBound { get; }
        public BoundExpression Step { get; }
        public BoundStatement Body { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

        public BoundForStatement(SyntaxNode syntax, VariableSymbol variable, 
            BoundExpression lowerBound,
            BoundExpression upperBound, 
            BoundExpression step,
            BoundStatement body, 
            BoundLabel breakLabel, 
            BoundLabel continueLabel)
            : base(syntax, breakLabel, continueLabel)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Step = step;
            Body = body;
        }
    }
}