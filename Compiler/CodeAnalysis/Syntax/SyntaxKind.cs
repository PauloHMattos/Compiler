namespace Compiler.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Tokens
        BadToken,
        EndOfFileToken,
        WhitespaceToken,
        SingleLineCommentToken,
        MultiLineCommentToken,
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
        PlusEqualsToken,
        MinusToken,
        MinusEqualsToken,
        StarToken,
        StarEqualsToken,
        SlashToken,
        SlashEqualsToken,
        PercentToken,
        BangToken,
        TildeToken,
        HatToken,
        HatEqualsToken,
        AmpersandToken,
        AmpersandEqualsToken,
        AmpersandAmpersandToken,
        PipeToken,
        PipeEqualsToken,
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
        CompoundAssignmentExpression,
        CallExpression,
    }
}
