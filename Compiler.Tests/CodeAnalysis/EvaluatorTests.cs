﻿using System;
using System.Collections.Generic;
using Compiler.CodeAnalysis;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Diagnostic;
using Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Compiler.Tests.CodeAnalysis
{
    public class EvaluatorTests
    {
        [Theory]
        [InlineData("1", 1)]
        [InlineData("+1", 1)]
        [InlineData("-1", -1)]
        [InlineData("+-1", -1)]
        [InlineData("1+2", 3)]
        [InlineData("1-2", -1)]
        [InlineData("1*2", 2)]
        [InlineData("100/10", 10)]
        [InlineData("(1 + 2)", 3)]
        [InlineData("1 == 2", false)]
        [InlineData("1 != 2", true)]
        [InlineData("1 < 2", true)]
        [InlineData("2 <= 2", true)]
        [InlineData("3 <= 4", true)]
        [InlineData("5 > 4", true)]
        [InlineData("4 > 3 * 2", false)]
        [InlineData("2 >= 2", true)]
        [InlineData("2 * 2 > 2", true)]
        [InlineData("1 + 1 == 2", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("true == true", true)]
        [InlineData("true || true", true)]
        [InlineData("true && true", true)]
        [InlineData("true == false", false)]
        [InlineData("true || false", true)]
        [InlineData("true && false", false)]
        [InlineData("false == false", true)]
        [InlineData("false || true", true)]
        [InlineData("false || false", false)]
        [InlineData("var a = 1", 1)]
        [InlineData("var a = true", true)]
        [InlineData("const a = 1", 1)]
        [InlineData("const a = true", true)]
        [InlineData("{ var a = 0 (a = 20) * a }", 400)]
        [InlineData("{ var a = 0 if a == 0 a = 10 }", 10)]
        [InlineData("{ var a = 5 if a == 0 a = 10 }", 5)]
        [InlineData("{ var a = 0 if a == 0 a = 10 else a = 20 }", 10)]
        [InlineData("{ var a = 5 if a == 0 a = 10 else a = 20 }", 20)]
        [InlineData("{ var a = 5 while a > 0 a = a - 1 a }", 0)]
        [InlineData("{ var a = 5 while a == 0 a = a - 1 a }", 5)]
        [InlineData("{ var a = 0 for i = 0 to 10 a = a + i a }", 55)]
        [InlineData("{ var a = 0 for i = 0 to 10 step 2 a = a + i a }", 30)]
        [InlineData("{ var a = 0 for i = 0 to -10 step -1 a = a + i a }", -55)]
        public void Evaluator_Compute_CorrectValues(string text, object expectedValue)
        {
            AssertValue(text, expectedValue);
        }

        [Fact]
        public void Evaluator_IfStatement_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 0
                    if [10]
                        x = 10
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotConvert.GetDiagnostic(TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_WhileStatement_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 0
                    while [10]
                        x = x + 1
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotConvert.GetDiagnostic(TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_ForStatement_Reports_CannotConvert_LowerBound()
        {
            var text = @"
                {
                    var result = 0
                    for i = [false] to 10
                        result = i + 1
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotConvert.GetDiagnostic(TypeSymbol.Bool, TypeSymbol.Int)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_ForStatement_Reports_CannotConvert_UpperBound()
        {
            var text = @"
                {
                    var result = 0
                    for i = 0 to [false]
                        result = i + 1
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotConvert.GetDiagnostic(TypeSymbol.Bool, TypeSymbol.Int)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_ForStatement_Reports_CannotConvert_Step()
        {
            var text = @"
                {
                    var result = 0
                    for i = 0 to 10 step [false]
                        result = i + 1
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotConvert.GetDiagnostic(TypeSymbol.Bool, TypeSymbol.Int)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_AssignmentExpression_Reports_Undefined()
        {
            var text = "[x] = 1";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedName.GetDiagnostic("x")
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_AssignmentExpression_Reports_CannotReassign()
        {
            var text = @"
                {
                    const x = 10
                    {
                        var x = 10
                    }
                    x [=] 5
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.VariableCannotReassigned.GetDiagnostic("x")
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_AssignmentExpression_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 10
                    x = [false]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotConvert.GetDiagnostic(TypeSymbol.Bool, TypeSymbol.Int)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_NameExpression_Reports_Undefined()
        {
            var text = "[x] * 1";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedName.GetDiagnostic("x")
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_UnaryExpression_Reports_UndefinedOperator()
        {
            var text = "[+]false";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedUnaryOperator.GetDiagnostic("+", TypeSymbol.Bool)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_BinaryExpression_Reports_UndefinedOperator()
        {
            var text = "10 [+] false";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic("+", TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_VariableDeclaration_Reports_Redeclaration()
        {
            var text = @"
                {
                    var x = 10
                    var y = 100
                    {
                        var x = 10
                    }
                    var [x] = 5
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.VariableAlreadyDeclared.GetDiagnostic("x")
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_NameExpression_Reports_NoErrorForInsertedToken()
        {
            var text = @"[]";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EndOfFileToken, SyntaxKind.IdentifierToken)
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_BlockStatement_NoInfiniteLoop()
        {
            var text = @"
                {
                [)][]
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.CloseParenthesisToken, SyntaxKind.IdentifierToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EndOfFileToken, SyntaxKind.CloseBraceToken)
            };
            
            AssertHasDiagnostics(text, diagnostics);
        }

        private static void AssertValue(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = new Compilation(syntaxTree);

            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }

        private void AssertHasDiagnostics(string text, List<string> expectedDiagnostics)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);

            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            if (annotatedText.Spans.Length != result.Diagnostics.Length)
            {
                throw new InvalidOperationException("ERROR: Must mark the same number os spans as there are expected diagnostics");
            }

            Assert.Equal(expectedDiagnostics.Count, result.Diagnostics.Length);
            for (var i = 0; i < result.Diagnostics.Length; i++)
            {
                Assert.Equal(annotatedText.Spans[i], result.Diagnostics[i].Span);
                Assert.Equal(expectedDiagnostics[i], result.Diagnostics[i].Message);
            }
        }
    }
}
