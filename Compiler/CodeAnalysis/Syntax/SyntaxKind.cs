namespace Compiler.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Tokens
        BadToken,
        EndOfFileToken,
        WhitespaceToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenBraceToken,
        CloseBraceToken,
        IdentifierToken,

        // Keywords
        FalseKeyword,
        TrueKeyword,
        ConstKeyword,
        VarKeyword,

        // Literals
        NumberToken,

        // Operators
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        BangToken,
        AmpersandAmpersandToken,
        PipePipeToken,
        EqualsToken,
        BangEqualsToken,
        EqualsEqualsToken,
        LessToken,
        LessOrEqualsToken,
        GreaterToken,
        GreaterOrEqualsToken,

        // Nodes
        CompilationUnit,

        // Statements
        BlockStatement,
        ExpressionStatement,
        VariableDeclarationStatement,

        // Expressions
        LiteralExpression,
        NameExpression,
        BinaryExpression,
        ParenthesizedExpression,
        UnaryExpression,
        AssignmentExpression,
    }
}
