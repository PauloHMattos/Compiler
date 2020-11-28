using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundVariableDeclarationStatement : BoundStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Initializer { get; }
        public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;

        public BoundVariableDeclarationStatement(SyntaxNode syntax, VariableSymbol variable, BoundExpression initializer)
            : base(syntax)
        {
            Variable = variable;
            Initializer = initializer;
        }
    }
}