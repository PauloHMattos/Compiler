using System;
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

        private void Report(in TextLocation location, string message)
        {
            _diagnostics.Add(new Diagnostic(location, message));
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
            Report(location, DiagnosticCode.BadCharacter.GetDiagnostic(current));
        }

        public void ReportInvalidLiteralType(in TextLocation location, string text, TypeSymbol type)
        {
            Report(location, DiagnosticCode.InvalidLiteralType.GetDiagnostic(text, type));
        }

        public void ReportUnterminatedString(in TextLocation location)
        {
            Report(location, DiagnosticCode.UnterminatedString.GetDiagnostic());
        }

        public void ReportUnexpectedToken(in TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            Report(location, DiagnosticCode.UnexpectedToken.GetDiagnostic(actualKind, expectedKind));
        }

        public void ReportUndefinedUnaryOperator(in TextLocation location, string operatorText, TypeSymbol operandType)
        {
            Report(location, DiagnosticCode.UndefinedUnaryOperator.GetDiagnostic(operatorText, operandType));
        }

        public void ReportUndefinedBinaryOperator(in TextLocation location, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            Report(location, DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic(operatorText, leftType, rightType));
        }

        public void ReportUndefinedVariable(in TextLocation location, string name)
        {
            Report(location, DiagnosticCode.UndefinedVariable.GetDiagnostic(name));
        }

        public void ReportNotAVariable(in TextLocation location, string name)
        {
            Report(location, DiagnosticCode.NotAVariable.GetDiagnostic(name));
        }

        public void ReportUndefinedType(in TextLocation location, string name)
        {
            Report(location, DiagnosticCode.UndefinedType.GetDiagnostic(name, name));
        }

        public void ReportCannotConvert(in TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            Report(location, DiagnosticCode.CannotConvert.GetDiagnostic(fromType, toType));
        }

        public void ReportCannotConvertImplicitly(in TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            Report(location, DiagnosticCode.CannotConvertImplicitly.GetDiagnostic(fromType, toType));
        }

        public void ReportSymbolAlreadyDeclared(in TextLocation location, string name)
        {
            Report(location, DiagnosticCode.SymbolAlreadyDeclared.GetDiagnostic(name));
        }

        public void ReportCannotReassigned(in TextLocation location, string name)
        {
            Report(location, DiagnosticCode.VariableCannotReassigned.GetDiagnostic(name));
        }

        public void ReportExpressionMustHaveValue(in TextLocation location)
        {
            Report(location, DiagnosticCode.ExpressionMustHaveValue.GetDiagnostic());
        }

        public void ReportUndefinedFunction(in TextLocation location, string name)
        {
            Report(location, DiagnosticCode.UndefinedFunction.GetDiagnostic(name));
        }

        public void ReportNotAFunction(TextLocation location, string name)
        {
            Report(location, DiagnosticCode.NotAFunction.GetDiagnostic(name));
        }

        public void ReportWrongArgumentCount(in TextLocation location, string name, in int expectedCount, in int actualCount)
        {
            Report(location, DiagnosticCode.WrongArgumentCount.GetDiagnostic(name, expectedCount, actualCount));
        }

        public void ReportWrongArgumentType(in TextLocation location, string name, TypeSymbol expectedType, TypeSymbol actualType)
        {
            Report(location, DiagnosticCode.WrongArgumentType.GetDiagnostic(name, expectedType, actualType));
        }

        public void ReportParameterAlreadyDeclared(in TextLocation location, string parameterName)
        {
            Report(location, DiagnosticCode.ParameterAlreadyDeclared.GetDiagnostic(parameterName));
        }

        public void ReportInvalidBreakOrContinue(in TextLocation location, string text)
        {
            Report(location, DiagnosticCode.InvalidBreakOrContinue.GetDiagnostic(text));
        }

        public void ReportInvalidReturn(in TextLocation location)
        {
            Report(location, DiagnosticCode.InvalidReturn.GetDiagnostic());
        }

        public void ReportInvalidReturnExpression(in TextLocation location, string functionName)
        {
            Report(location, DiagnosticCode.InvalidReturnExpression.GetDiagnostic(functionName));
        }

        public void ReportMissingReturnExpression(in TextLocation location, TypeSymbol returnType)
        {
            Report(location, DiagnosticCode.MissingReturnExpression.GetDiagnostic(returnType));
        }

        public void ReportAllPathsMustReturn(in TextLocation location)
        {
            Report(location, DiagnosticCode.AllPathsMustReturn.GetDiagnostic());
        }

        internal void ReportInvalidExpressionStatement(in TextLocation location)
        {
            Report(location, DiagnosticCode.InvalidExpressionStatement.GetDiagnostic());
        }
    }
}