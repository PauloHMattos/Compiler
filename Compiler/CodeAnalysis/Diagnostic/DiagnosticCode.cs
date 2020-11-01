namespace Compiler.CodeAnalysis.Diagnostic
{
    public enum DiagnosticCode
    {
        BadCharacter,
        InvalidLiteralType,
        UnexpectedToken,
        UndefinedUnaryOperator,
        UndefinedBinaryOperator,
        UndefinedName,
        CannotConvert,
        VariableAlreadyDeclared,
        VariableCannotReassigned
    }
}