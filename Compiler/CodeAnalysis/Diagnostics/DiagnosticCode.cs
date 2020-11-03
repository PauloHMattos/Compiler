using System.Collections.Generic;

namespace Compiler.CodeAnalysis.Diagnostics
{
    public enum DiagnosticCode
    {
        BadCharacter,
        InvalidLiteralType,
        UnterminatedString,
        UnexpectedToken,
        UndefinedUnaryOperator,
        UndefinedBinaryOperator,
        UndefinedVariable,
        NotAVariable,
        CannotConvert,
        SymbolAlreadyDeclared,
        VariableCannotReassigned,
        ExpressionMustHaveValue,
        UndefinedFunction,
        NotAFunction,
        WrongArgumentCount,
        WrongArgumentType,
    }


    public static class DiagnosticCodeExtension
    {
        private static readonly Dictionary<DiagnosticCode, string> Messages = new Dictionary<DiagnosticCode, string>
        {
            {DiagnosticCode.BadCharacter, "Bad character in input: '{0}'."},
            {DiagnosticCode.InvalidLiteralType, "The literal {0} isn't a valid {1}."},
            {DiagnosticCode.UnterminatedString, "Unterminated string literal."},
            {DiagnosticCode.UnexpectedToken, "Unexpected token <{0}>, expected <{1}>."},
            {DiagnosticCode.UndefinedUnaryOperator, "Unary operator '{0}' is not defined for type '{1}'."},
            {DiagnosticCode.UndefinedBinaryOperator, "Binary operator '{0}' is not defined for types '{1}' and '{2}'."},
            {DiagnosticCode.UndefinedVariable, "Variable '{0}' is not defined."},
            {DiagnosticCode.NotAVariable, "'{0}' is not a variable."},
            {DiagnosticCode.CannotConvert, "Cannot convert type '{0}' to {1}."},
            {DiagnosticCode.SymbolAlreadyDeclared, "'{0}' has already been declared."},
            {DiagnosticCode.VariableCannotReassigned, "Variable '{0}' is const and cannot be reassigned."},
            {DiagnosticCode.ExpressionMustHaveValue, "Expression must have a value."},
            {DiagnosticCode.UndefinedFunction, "Function '{0}' doesn't exist."},
            {DiagnosticCode.NotAFunction, "'{0}' is not a function."},
            {DiagnosticCode.WrongArgumentCount, "Function '{0}' requires {1} arguments but was given {2}."},
            {DiagnosticCode.WrongArgumentType, "Parameter '{0}' requires a value of type '{1}' but was given a value of type '{2}'."},
        };

        public static string GetDiagnostic(this DiagnosticCode code, params object[] arguments)
        {
            var diagnosticFormat = Messages[code];
            var diagnostic = string.Format(diagnosticFormat, arguments);
            return diagnostic;
        }
    }
}