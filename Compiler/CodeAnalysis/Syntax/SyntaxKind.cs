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
        IdentifierToken,

        // Keywords
        FalseKeywordToken,
        TrueKeywordToken,

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

        // Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesizedExpression,
        UnaryExpression,
    }
}
