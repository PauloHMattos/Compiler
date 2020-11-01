namespace Compiler.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statements
        BlockStatement,
        ExpressionStatement,

        LiteralExpression,
        UnaryExpression,
        BinaryExpression,
        VariableExpression,
        AssignmentExpression,
    }
}