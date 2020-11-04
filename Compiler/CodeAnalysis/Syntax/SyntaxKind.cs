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
        CommaToken,
        ColonToken,
        IdentifierToken,

        // Keywords
        FalseKeyword,
        TrueKeyword,
        ConstKeyword,
        VarKeyword,
        IfKeyword,
        ElseKeyword,
        DoKeyword,
        WhileKeyword,
        ForKeyword,
        ToKeyword,
        StepKeyword,
        ContinueKeyword,
        BreakKeyword,
        FunctionKeyword,
        ReturnKeyword,

        // Literals
        NumberToken,
        StringToken,

        // Operators
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        BangToken,
        TildeToken,
        HatToken,
        AmpersandToken,
        AmpersandAmpersandToken,
        PipeToken,
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
        FunctionDeclaration,
        GlobalStatement,
        Parameter,
        TypeClause,
        ElseClause,
        StepClause,

        // Statements
        BlockStatement,
        ExpressionStatement,
        VariableDeclarationStatement,
        IfStatement,
        DoWhileStatement,
        WhileStatement,
        ForStatement,
        ContinueStatement,
        BreakStatement,
        ReturnStatement,

        // Expressions
        LiteralExpression,
        NameExpression,
        BinaryExpression,
        ParenthesizedExpression,
        UnaryExpression,
        AssignmentExpression,
        CallExpression,
    }
}
