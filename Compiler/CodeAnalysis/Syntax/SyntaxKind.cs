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

        // Literals
        NumberToken,

        // Operators
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,

        // Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesizedExpression,
        UnaryExpression
    }
}
