using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;
using Mono.Cecil;

namespace Compiler.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics;

        public DiagnosticBag()
        {
            _diagnostics = new List<Diagnostic>();
        }

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void ReportError(in TextLocation location, string message)
        {
            _diagnostics.Add(Diagnostic.Error(location, message));
        }

        private void ReportWarning(in TextLocation location, string message)
        {
            _diagnostics.Add(Diagnostic.Warning(location, message));
        }

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                _diagnostics.Add(diagnostic);
            }
        }

        public void ReportBadCharacter(in TextLocation location, in char current)
        {
            ReportError(location, DiagnosticCode.BadCharacter.GetDiagnostic(current));
        }

        internal void ReportInvalidReference(string referencePath)
        {
            ReportError(new TextLocation(), DiagnosticCode.InvalidReference.GetDiagnostic(referencePath));
        }

        public void ReportInvalidLiteralType(in TextLocation location, string text, TypeSymbol type)
        {
            ReportError(location, DiagnosticCode.InvalidLiteralType.GetDiagnostic(text, type));
        }

        public void ReportUnterminatedString(in TextLocation location)
        {
            ReportError(location, DiagnosticCode.UnterminatedString.GetDiagnostic());
        }

        public void ReportUnexpectedToken(in TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            ReportError(location, DiagnosticCode.UnexpectedToken.GetDiagnostic(actualKind, expectedKind));
        }

        public void ReportUndefinedUnaryOperator(in TextLocation location, string operatorText, TypeSymbol operandType)
        {
            ReportError(location, DiagnosticCode.UndefinedUnaryOperator.GetDiagnostic(operatorText, operandType));
        }

        public void ReportUndefinedBinaryOperator(in TextLocation location, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            ReportError(location, DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic(operatorText, leftType, rightType));
        }

        public void ReportUndefinedVariable(in TextLocation location, string name)
        {
            ReportError(location, DiagnosticCode.UndefinedVariable.GetDiagnostic(name));
        }

        internal void ReportRequiredTypeNotFound(string? name, string metadataName)
        {
            name = name == null
                ? $"'{metadataName}'"
                : $"'{name}' ('{metadataName}')";

            ReportError(default, DiagnosticCode.RequiredTypeNotFound.GetDiagnostic(name));
        }

        internal void ReportRequiredTypeAmbiguous(string? name, string metadataName, TypeDefinition[] foundTypes)
        {
            var assemblyNames = foundTypes.Select(t => t.Module.Assembly.Name.Name);
            var assemblyNameList = string.Join(", ", assemblyNames);

            name = name == null
                ? $"'{metadataName}'"
                : $"'{name}' ('{metadataName}')";

            ReportError(default, DiagnosticCode.RequiredTypeAmbiguous.GetDiagnostic(name, assemblyNameList));
        }

        internal void ReportRequiredMethodNotFound(string typeName, string methodName, Type[] parameterTypes)
        {
            var parameterTypeNameList = string.Join(", ", parameterTypes.Select(t => t.FullName));
            ReportError(default, DiagnosticCode.RequiredMethodNotFound.GetDiagnostic(typeName, methodName, parameterTypeNameList));
        }

        public void ReportNotAVariable(in TextLocation location, string name)
        {
            ReportError(location, DiagnosticCode.NotAVariable.GetDiagnostic(name));
        }

        public void ReportUndefinedType(in TextLocation location, string name)
        {
            ReportError(location, DiagnosticCode.UndefinedType.GetDiagnostic(name, name));
        }

        public void ReportCannotConvert(in TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            ReportError(location, DiagnosticCode.CannotConvert.GetDiagnostic(fromType, toType));
        }

        public void ReportCannotConvertImplicitly(in TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            ReportError(location, DiagnosticCode.CannotConvertImplicitly.GetDiagnostic(fromType, toType));
        }

        public void ReportSymbolAlreadyDeclared(in TextLocation location, string name)
        {
            ReportError(location, DiagnosticCode.SymbolAlreadyDeclared.GetDiagnostic(name));
        }

        public void ReportCannotReassigned(in TextLocation location, string name)
        {
            ReportError(location, DiagnosticCode.VariableCannotReassigned.GetDiagnostic(name));
        }

        public void ReportExpressionMustHaveValue(in TextLocation location)
        {
            ReportError(location, DiagnosticCode.ExpressionMustHaveValue.GetDiagnostic());
        }

        public void ReportUndefinedFunction(in TextLocation location, string name)
        {
            ReportError(location, DiagnosticCode.UndefinedFunction.GetDiagnostic(name));
        }

        public void ReportNotAFunction(TextLocation location, string name)
        {
            ReportError(location, DiagnosticCode.NotAFunction.GetDiagnostic(name));
        }

        public void ReportWrongArgumentCount(in TextLocation location, string name, in int expectedCount, in int actualCount)
        {
            ReportError(location, DiagnosticCode.WrongArgumentCount.GetDiagnostic(name, expectedCount, actualCount));
        }

        public void ReportParameterAlreadyDeclared(in TextLocation location, string parameterName)
        {
            ReportError(location, DiagnosticCode.ParameterAlreadyDeclared.GetDiagnostic(parameterName));
        }

        public void ReportMemberAlreadyDeclared(in TextLocation location, string typeName, string memberName)
        {
            ReportError(location, DiagnosticCode.AlreadyDeclaredMember.GetDiagnostic(typeName, memberName));
        }

        public void ReportEnumerationAlreadyContainsValue(in TextLocation location, string repeatedEnumName, int value, string originalName)
        {
            ReportWarning(location, DiagnosticCode.EnumerationAlreadyContainsValue.GetDiagnostic(repeatedEnumName, value, originalName));
        }

        public void ReportInvalidBreakOrContinue(in TextLocation location, string text)
        {
            ReportError(location, DiagnosticCode.InvalidBreakOrContinue.GetDiagnostic(text));
        }

        public void ReportInvalidReturnExpression(in TextLocation location, string functionName)
        {
            ReportError(location, DiagnosticCode.InvalidReturnExpression.GetDiagnostic(functionName));
        }

        public void ReportMissingReturnExpression(in TextLocation location, TypeSymbol returnType)
        {
            ReportError(location, DiagnosticCode.MissingReturnExpression.GetDiagnostic(returnType));
        }

        public void ReportAllPathsMustReturn(in TextLocation location)
        {
            ReportError(location, DiagnosticCode.AllPathsMustReturn.GetDiagnostic());
        }

        public void ReportInvalidExpressionStatement(in TextLocation location)
        {
            ReportError(location, DiagnosticCode.InvalidExpressionStatement.GetDiagnostic());
        }

        public void ReportOnlyOneFileCanHaveGlobalStatements(in TextLocation location)
        {
            ReportError(location, DiagnosticCode.OnlyOneFileCanHaveGlobalStatements.GetDiagnostic());
        }

        public void ReportMainMustHaveCorrectSignature(in TextLocation location)
        {
            ReportError(location, DiagnosticCode.MainMustHaveCorrectSignature.GetDiagnostic());
        }

        public void ReportCannotMixMainAndGlobalStatements(in TextLocation location)
        {
            ReportError(location, DiagnosticCode.CannotMixMainAndGlobalStatements.GetDiagnostic());
        }

        internal void ReportInvalidReturnWithValueInGlobalStatements(TextLocation location)
        {
            ReportError(location, DiagnosticCode.InvalidReturnWithValueInGlobalStatements.GetDiagnostic());
        }

        internal void ReportUnterminatedMultilineComment(TextLocation location)
        {
            ReportError(location, DiagnosticCode.UnterminatedMultilineComment.GetDiagnostic());
        }
    }
}