namespace Compiler.CodeAnalysis
{
    public enum SyntaxKind
    {
        // Tokens
        EndOfFileToken,
        BadToken,
        NumberToken,
        WhitespaceToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        OpenParenthesisToken,
        CloseParenthesisToken,

        // Expressions
        NumberExpression,
        BinaryExpression,
        ParenthesizedExpression
    }
}
