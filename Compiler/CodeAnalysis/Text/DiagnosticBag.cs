using System;
using System.Collections;
using System.Collections.Generic;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Text
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
            var messageFormat = Messages[DiagnosticCode.BadCharacter];
            var message = string.Format(messageFormat, current);
            Report(span, message);
        }

        public void ReportInvalidLiteralType(in TextSpan span, string text, Type type)
        {
            var messageFormat = Messages[DiagnosticCode.InvalidLiteralType];
            var message = string.Format(messageFormat, text, type);
            Report(span, message);
        }

        public void ReportUnexpectedToken(in TextSpan span, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            var messageFormat = Messages[DiagnosticCode.UnexpectedToken];
            var message = string.Format(messageFormat, actualKind, expectedKind);
            Report(span, message);
        }

        public void ReportUndefinedUnaryOperator(in TextSpan span, string operatorText, Type operandType)
        {
            var messageFormat = Messages[DiagnosticCode.UndefinedUnaryOperator];
            var message = string.Format(messageFormat, operatorText, operandType);
            Report(span, message);
        }

        public void ReportUndefinedBinaryOperator(in TextSpan span, string operatorText, Type leftType, Type rightType)
        {
            var messageFormat = Messages[DiagnosticCode.UndefinedBinaryOperator];
            var message = string.Format(messageFormat, operatorText, leftType, rightType);
            Report(span, message);
        }

        private static readonly Dictionary<DiagnosticCode, string> Messages = new Dictionary<DiagnosticCode, string>
        {
            {DiagnosticCode.BadCharacter, "Bad character in input: '{0}'."},
            {DiagnosticCode.InvalidLiteralType, "The literal {0} isn't a valid {1}."},
            {DiagnosticCode.UnexpectedToken, "Unexpected token <{0}>, expected <{1}>."},
            {DiagnosticCode.UndefinedUnaryOperator, "Unary operator '{0}' is not defined for type '{1}'."},
            {DiagnosticCode.UndefinedBinaryOperator, "Binary operator '{0}' is not defined for types '{1}' and '{2}'."},
        };
    }
}