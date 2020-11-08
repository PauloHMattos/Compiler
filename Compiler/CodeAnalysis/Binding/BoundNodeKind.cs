namespace Compiler.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statements
        BlockStatement,
        NopStatement,
        ExpressionStatement,
        VariableDeclarationStatement,
        IfStatement,
        DoWhileStatement,
        WhileStatement,
        ForStatement,
        LabelStatement,
        GotoStatement,
        ConditionalGotoStatement,
        ReturnStatement,

        // Expressions
        ErrorExpression,
        LiteralExpression,
        UnaryExpression,
        BinaryExpression,
        VariableExpression,
        AssignmentExpression,
        CompoundAssignmentExpression,
        CallExpression,
        ConversionExpression
    }
}