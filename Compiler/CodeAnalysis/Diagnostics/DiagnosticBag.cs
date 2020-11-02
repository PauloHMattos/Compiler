using System;
using System.Collections;
using System.Collections.Generic;
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

        public void ReportInvalidLiteralType(in TextSpan span, string text, Type type)
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

        public void ReportUndefinedUnaryOperator(in TextSpan span, string operatorText, Type operandType)
        {
            Report(span, DiagnosticCode.UndefinedUnaryOperator.GetDiagnostic(operatorText, operandType));
        }

        public void ReportUndefinedBinaryOperator(in TextSpan span, string operatorText, Type leftType, Type rightType)
        {
            Report(span, DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic(operatorText, leftType, rightType));
        }

        public void ReportUndefinedName(in TextSpan span, string name)
        {
            Report(span, DiagnosticCode.UndefinedName.GetDiagnostic(name));
        }
        
        public void ReportCannotConvert(in TextSpan span, Type fromType, Type toType)
        {
            Report(span, DiagnosticCode.CannotConvert.GetDiagnostic(fromType, toType));
        }

        public void ReportVariableAlreadyDeclared(in TextSpan span, string name)
        {
            Report(span, DiagnosticCode.VariableAlreadyDeclared.GetDiagnostic(name));
        }

        public void ReportCannotReassigned(in TextSpan span, string name)
        {
            Report(span, DiagnosticCode.VariableCannotReassigned.GetDiagnostic(name));
        }
    }
}