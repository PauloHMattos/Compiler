namespace Compiler.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statements
        BlockStatement,
        ExpressionStatement,
        VariableDeclarationStatement,
        IfStatement,

        LiteralExpression,
        UnaryExpression,
        BinaryExpression,
        VariableExpression,
        AssignmentExpression,
    }
}