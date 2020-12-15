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
        NotAVariable,
        CannotConvert,
        SymbolAlreadyDeclared,
        VariableCannotReassigned,
        ExpressionMustHaveValue,
        UndefinedFunction,
        NotAFunction,
        WrongArgumentCount,
        UndefinedType,
        CannotConvertImplicitly,
        ParameterAlreadyDeclared,
        FunctionsAreUnsupported,
        InvalidBreakOrContinue,
        InvalidReturnExpression,
        MissingReturnExpression,
        AllPathsMustReturn,
        InvalidExpressionStatement,
        InvalidGlobalStatement,
        MainMustHaveCorrectSignature,
        InvalidReturnWithValueInGlobalStatements,
        InvalidReference,
        RequiredTypeNotFound,
        RequiredTypeAmbiguous,
        RequiredMethodNotFound,
        UnterminatedMultilineComment,
        AlreadyDeclaredMember,
        EnumerationAlreadyContainsValue,
        CannotAccessMember,
        UndefinedOverloadForArguments,
        CannotUseSelfOutsideOfAFunction,
        CannotUseSelfOutsideOfReceiverFunctions,
        UnreachableCode,
        CannotAssignMethod,
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
            {DiagnosticCode.UndefinedName, "The name '{0}' does not exist in the current context."},
            {DiagnosticCode.NotAVariable, "'{0}' is not a variable."},
            {DiagnosticCode.CannotConvert, "Cannot convert type '{0}' to '{1}'."},
            {DiagnosticCode.SymbolAlreadyDeclared, "'{0}' has already been declared."},
            {DiagnosticCode.VariableCannotReassigned, "Variable '{0}' is const and cannot be reassigned."},
            {DiagnosticCode.ExpressionMustHaveValue, "Expression must have a value."},
            {DiagnosticCode.UndefinedFunction, "Function '{0}' doesn't exist."},
            {DiagnosticCode.NotAFunction, "'{0}' is not a function."},
            {DiagnosticCode.WrongArgumentCount, "Function '{0}' requires {1} arguments but was given {2}."},
            {DiagnosticCode.UndefinedType, "Type '{0}' doesn't exist."},
            {DiagnosticCode.CannotConvertImplicitly, "Cannot convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)"},
            {DiagnosticCode.ParameterAlreadyDeclared, "A parameter with the name '{0}' already exists."},
            {DiagnosticCode.FunctionsAreUnsupported, "Functions with return values are unsupported."},
            {DiagnosticCode.InvalidBreakOrContinue, "The keyword '{0}' can only be used inside of loops."},
            {DiagnosticCode.InvalidReturnExpression, "Since the function '{0}' does not return a value the 'return' keyword cannot be followed by an expression."},
            {DiagnosticCode.MissingReturnExpression, "An expression of type '{0}' is expected."},
            {DiagnosticCode.AllPathsMustReturn, "Not all code paths return a value."},
            {DiagnosticCode.InvalidExpressionStatement, "Only assignment and call expressions can be used as a statement."},
            {DiagnosticCode.InvalidGlobalStatement, "Invalid global statement."},
            {DiagnosticCode.MainMustHaveCorrectSignature, "main must not take arguments and not return anything."},
            {DiagnosticCode.InvalidReturnWithValueInGlobalStatements, "The 'return' keyword cannot be followed by an expression in global statements."},
            {DiagnosticCode.InvalidReference, "The reference is not a valid .NET assembly {0}"},
            {DiagnosticCode.RequiredTypeNotFound, "The required type {0} cannot be resolved among the given references."},
            {DiagnosticCode.RequiredTypeAmbiguous, "The required type {0} was found in multiple references: {1}"},
            {DiagnosticCode.RequiredMethodNotFound, "The required method '{0}.{1}({2})' cannot be resolved among the given references."},
            {DiagnosticCode.UnterminatedMultilineComment, "Unterminated multi-line comment."},
            {DiagnosticCode.AlreadyDeclaredMember, "The type '{0}' already contains a definition for '{1}'."},
            {DiagnosticCode.EnumerationAlreadyContainsValue, "The enumeration member '{0}' has the same constant value '{1}' as the member '{2}'."},
            {DiagnosticCode.CannotAccessMember, "Cannot access member '{0}' of type '{1}'."},
            {DiagnosticCode.UndefinedOverloadForArguments, "No overload of '{0}' for arguments '{1}' was found."},
            {DiagnosticCode.CannotUseSelfOutsideOfAFunction, "This can only by used in functions with a struct receiver."},
            {DiagnosticCode.CannotUseSelfOutsideOfReceiverFunctions, "This can only be used in functions with a struct receiver.  Function '{0}' has no receiver defined."},
            {DiagnosticCode.UnreachableCode, "Unreachable code detected."},
            {DiagnosticCode.CannotAssignMethod, "Cannot assign method member {0} of type {1}."},
        };

        public static string GetDiagnostic(this DiagnosticCode code, params object[] arguments)
        {
            var diagnosticFormat = Messages[code];
            var diagnostic = string.Format(diagnosticFormat, arguments);
            return diagnostic;
        }
    }
}