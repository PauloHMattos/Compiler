using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundVariableDeclarationStatement : BoundStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Initializer { get; }
        public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;

        public BoundVariableDeclarationStatement(VariableSymbol variable, BoundExpression initializer)
        {
            Variable = variable;
            Initializer = initializer;
        }
    }
}