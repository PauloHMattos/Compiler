namespace Compiler.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        BadToken,

        // Trivia
        SkippedTextTrivia,
        WhitespaceTrivia,
        LineBreakTrivia,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,

        // Tokens
        EndOfFileToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenBraceToken,
        CloseBraceToken,
        CommaToken,
        ColonToken,
        IdentifierToken,
        DotToken,

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
        DefaultKeyword,
        EnumKeyword,
        StructKeyword,

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
        EnumDeclaration,
        EnumValue,
        StructDeclaration,
        MemberBlockStatement,
        GlobalStatement,
        Parameter,
        TypeClause,
        ElseClause,
        StepClause,
        EnumValueClause,

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
        CallExpression,
        MemberAccessExpression,
    }
}
