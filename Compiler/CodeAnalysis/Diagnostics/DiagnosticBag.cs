using System.Collections;
using System.Collections.Generic;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;

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

        private void Report(in TextSpan span, string message)
        {
            _diagnostics.Add(new Diagnostic(span, message));
        }

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                _diagnostics.Add(diagnostic);
            }
        }

        public void ReportBadCharacter(in TextSpan span, in char current)
        {
            Report(span, DiagnosticCode.BadCharacter.GetDiagnostic(current));
        }

        public void ReportInvalidLiteralType(in TextSpan span, string text, TypeSymbol type)
        {
            Report(span, DiagnosticCode.InvalidLiteralType.GetDiagnostic(text, type));
        }

        public void ReportUnterminatedString(TextSpan textSpan)
        {
            Report(textSpan, DiagnosticCode.UnterminatedString.GetDiagnostic());
        }

        public void ReportUnexpectedToken(in TextSpan span, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            Report(span, DiagnosticCode.UnexpectedToken.GetDiagnostic(actualKind, expectedKind));
        }

        public void ReportUndefinedUnaryOperator(in TextSpan span, string operatorText, TypeSymbol operandType)
        {
            Report(span, DiagnosticCode.UndefinedUnaryOperator.GetDiagnostic(operatorText, operandType));
        }

        public void ReportUndefinedBinaryOperator(in TextSpan span, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            Report(span, DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic(operatorText, leftType, rightType));
        }

        public void ReportUndefinedVariable(in TextSpan span, string name)
        {
            Report(span, DiagnosticCode.UndefinedVariable.GetDiagnostic(name));
        }

        public void ReportNotAVariable(TextSpan span, string name)
        {
            Report(span, DiagnosticCode.NotAVariable.GetDiagnostic(name));
        }

        public void ReportUndefinedType(TextSpan span, string name)
        {
            Report(span, DiagnosticCode.UndefinedType.GetDiagnostic(name, name));
        }

        public void ReportCannotConvert(in TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            Report(span, DiagnosticCode.CannotConvert.GetDiagnostic(fromType, toType));
        }

        public void ReportCannotConvertImplicitly(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            Report(span, DiagnosticCode.CannotConvertImplicitly.GetDiagnostic(fromType, toType));
        }

        public void ReportSymbolAlreadyDeclared(in TextSpan span, string name)
        {
            Report(span, DiagnosticCode.SymbolAlreadyDeclared.GetDiagnostic(name));
        }

        public void ReportCannotReassigned(in TextSpan span, string name)
        {
            Report(span, DiagnosticCode.VariableCannotReassigned.GetDiagnostic(name));
        }

        public void ReportExpressionMustHaveValue(in TextSpan span)
        {
            Report(span, DiagnosticCode.ExpressionMustHaveValue.GetDiagnostic());
        }

        public void ReportUndefinedFunction(in TextSpan span, string name)
        {
            Report(span, DiagnosticCode.UndefinedFunction.GetDiagnostic(name));
        }

        public void ReportNotAFunction(TextSpan span, string name)
        {
            Report(span, DiagnosticCode.NotAFunction.GetDiagnostic(name));
        }

        public void ReportWrongArgumentCount(in TextSpan span, string name, in int expectedCount, in int actualCount)
        {
            Report(span, DiagnosticCode.WrongArgumentCount.GetDiagnostic(name, expectedCount, actualCount));
        }

        public void ReportWrongArgumentType(in TextSpan span, string name, TypeSymbol expectedType, TypeSymbol actualType)
        {
            Report(span, DiagnosticCode.WrongArgumentType.GetDiagnostic(name, expectedType, actualType));
        }

        public void ReportParameterAlreadyDeclared(TextSpan span, string parameterName)
        {
            Report(span, DiagnosticCode.ParameterAlreadyDeclared.GetDiagnostic(parameterName));
        }

        public void ReportInvalidBreakOrContinue(TextSpan span, string text)
        {
            Report(span, DiagnosticCode.InvalidBreakOrContinue.GetDiagnostic(text));
        }

        public void XXX_ReportFunctionsAreUnsupported(TextSpan span)
        {
            Report(span, DiagnosticCode.FunctionsAreUnsupported.GetDiagnostic());
        }
    }
}