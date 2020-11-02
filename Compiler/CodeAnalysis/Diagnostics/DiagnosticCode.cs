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
        UndefinedName,
        CannotConvert,
        VariableAlreadyDeclared,
        VariableCannotReassigned
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
            {DiagnosticCode.UndefinedName, "Variable '{0}' is not defined."},
            {DiagnosticCode.CannotConvert, "Cannot convert type '{0}' to {1}."},
            {DiagnosticCode.VariableAlreadyDeclared, "Variable '{0}' has already been declared."},
            {DiagnosticCode.VariableCannotReassigned, "Variable '{0}' is const and cannot be reassigned."},
        };

        public static string GetDiagnostic(this DiagnosticCode code, params object[] arguments)
        {
            var diagnosticFormat = Messages[code];
            var diagnostic = string.Format(diagnosticFormat, arguments);
            return diagnostic;
        }
    }
}