using System;
using System.Collections.Generic;
using Compiler.CodeAnalysis;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
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
        [InlineData("~1", -2)]
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
        [InlineData("1 | 2", 3)]
        [InlineData("1 | 0", 1)]
        [InlineData("1 & 3", 1)]
        [InlineData("1 & 0", 0)]
        [InlineData("1 ^ 0", 1)]
        [InlineData("0 ^ 1", 1)]
        [InlineData("1 ^ 3", 2)]
        [InlineData("3 % 1", 0)]
        [InlineData("2 % 2", 0)]
        [InlineData("1 % 2", 1)]
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
        [InlineData("false | false", false)]
        [InlineData("false | true", true)]
        [InlineData("true | false", true)]
        [InlineData("true | true", true)]
        [InlineData("false & false", false)]
        [InlineData("false & true", false)]
        [InlineData("true & false", false)]
        [InlineData("true & true", true)]
        [InlineData("false ^ false", false)]
        [InlineData("true ^ false", true)]
        [InlineData("false ^ true", true)]
        [InlineData("true ^ true", false)]
        [InlineData("{ var a = 1 return a }", 1)]
        [InlineData("{ var a = true return a }", true)]
        [InlineData("{ const a = 1 return a }", 1)]
        [InlineData("{ const a = true return a }", true)]
        [InlineData("\"test\"", "test")]
        [InlineData("\"te\"\"st\"", "te\"st")]
        [InlineData("\"hello \" + \"world\"", "hello world")]
        [InlineData("\"test\" == \"test\"", true)]
        [InlineData("\"test\" != \"test\"", false)]
        [InlineData("\"test\" == \"abc\"", false)]
        [InlineData("\"test\" != \"abc\"", true)]
        [InlineData("{ var a : any = 0 var b : any = \"b\" return a == b }", false)]
        [InlineData("{ var a : any = 0 var b : any = \"b\" return a != b }", true)]
        [InlineData("{ var a : any = 0 var b : any = 0 return a == b }", true)]
        [InlineData("{ var a : any = 0 var b : any = 0 return a != b }", false)]
        [InlineData("{ var a = 1 a += (2 + 3) return a }", 6)]
        [InlineData("{ var a = 1 a -= (2 + 3) return a }", -4)]
        [InlineData("{ var a = 1 a *= (2 + 3) return a }", 5)]
        [InlineData("{ var a = 1 a /= (2 + 3) return a }", 0)]
        [InlineData("{ var a = true a &= (false) return a }", false)]
        [InlineData("{ var a = true a |= (false) return a }", true)]
        [InlineData("{ var a = true a ^= (true) return a }", false)]
        [InlineData("{ var a = 1 a |= 0 return a }", 1)]
        [InlineData("{ var a = 1 a &= 3 return a }", 1)]
        [InlineData("{ var a = 1 a &= 0 return a }", 0)]
        [InlineData("{ var a = 1 a ^= 0 return a }", 1)]
        [InlineData("{ var a = 1 var b = 2 var c = 3 a += b += c return a }", 6)]
        [InlineData("{ var a = 1 var b = 2 var c = 3 a += b += c return b }", 5)]
        [InlineData("{ var a = 0 return (a = 20) * a }", 400)]
        [InlineData("{ var a = 0 if a == 0 a = 10 return a }", 10)]
        [InlineData("{ var a = 5 if a == 0 a = 10 return a }", 5)]
        [InlineData("{ var a = 0 if a == 0 a = 10 else a = 20 return a }", 10)]
        [InlineData("{ var a = 5 if a == 0 a = 10 else a = 20 return a }", 20)]
        [InlineData("{ var a = 5 do a = a - 1 while a > 0  return a }", 0)]
        [InlineData("{ var a = 5 while a > 0 a = a - 1 return a }", 0)]
        [InlineData("{ var a = 5 while a == 0 a = a - 1 return a }", 5)]
        [InlineData("{ var a = 0 for i = 0 to 10 a = a + i return a }", 55)]
        [InlineData("{ var a = 0 for i = 0 to 10 step 2 a = a + i return a }", 30)]
        [InlineData("{ var a = 0 for i = 0 to 10 break return a }", 0)]
        [InlineData("{ var a = 0 for i = 0 to 10 { a = a + i continue } return a }", 55)]
        [InlineData("{ var a = 0 for i = 0 to 10 { continue a = a + i } return a }", 0)]
        [InlineData("{ var i = 0 while i < 5 { i = i + 1 if i == 5 continue } return i }", 5)]
        [InlineData("{ var i = 0 do { i = i + 1 if i == 5 continue } while i < 5 return i }", 5)]
        //[InlineData("{ var a = 0 for i = 0 to -10 step -1 a = a + i return a }", -55)] // Currently we don't support reversed loops
        [InlineData("bool(\"false\")", false)]
        [InlineData("bool(\"true\")", true)]
        [InlineData("string(10)", "10")]
        [InlineData("string(true)", "True")]
        [InlineData("int(\"100\")", 100)]
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
        public void Evaluator_DoWhileStatement_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 0
                    do
                        x = 10
                    while [10]
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
        public void Evaluator_BreakOrContinueStatement_Reports_InvalidBreakOrContinue()
        {
            var text = @"
                {
                    [break]
                    [continue]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.InvalidBreakOrContinue.GetDiagnostic("break"),
                DiagnosticCode.InvalidBreakOrContinue.GetDiagnostic("continue")
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_AssignmentExpression_Reports_Undefined()
        {
            var text = "[x] = 1";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedVariable.GetDiagnostic("x")
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
        public void Evaluator_AssignmentExpression_Reports_CannotConvert_Implicitly()
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
        public void Evaluator_AssignmentExpression_Reports_CannotConvert_Explicit()
        {
            var text = @"
                {
                    var x : int = 10
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
        public void Evaluator_AssignmentExpression_Reports_NotAVariable()
        {
            var text = @"[print] = 42";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.NotAVariable.GetDiagnostic("print")
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_NameExpression_Reports_Undefined()
        {
            var text = "[x] * 1";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedVariable.GetDiagnostic("x")
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
        public void Evaluator_CompoundExpression_Reports_Undefined()
        {
            var text = @"var x = 10
                         x [+=] false";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic("+=", TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertHasDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Evaluator_CompoundExpression_Assignemnt_NonDefinedVariable_Reports_Undefined()
        {
            var text = @"[x] += 10";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedVariable.GetDiagnostic("x")
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_CompoundDeclarationExpression_Reports_CannotAssign()
        {
            var text = @"
                {
                    const x = 10
                    x [+=] 1
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.VariableCannotReassigned.GetDiagnostic("x")
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
                DiagnosticCode.SymbolAlreadyDeclared.GetDiagnostic("x")
            };
            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_NameExpression_Reports_NoErrorForInsertedToken()
        {
            var text = @"1 + []";

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

        [Fact]
        public void Evaluator_Script_Return()
        {
            var text = @"
                return
            ";

            AssertValue(text, "");
        }

        [Fact]
        public void Evaluator_Void_Function_Should_Not_Return_Value()
        {
            var text = @"
                function test()
                {
                    return [1]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.InvalidReturnExpression.GetDiagnostic("test"),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Function_With_ReturnValue_Should_Not_Return_Void()
        {
            var text = @"
                function test(): int
                {
                    [return]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.MissingReturnExpression.GetDiagnostic(TypeSymbol.Int),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_FunctionReturn_Missing()
        {
            var text = @"
                function [add](a: int, b: int): int
                {
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.AllPathsMustReturn.GetDiagnostic(TypeSymbol.Int),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Parameter_Already_Declared()
        {
            var text = @"
                function sum(a: int, b: int, [a: int]) : int
                {
                    return a + b + c
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.ParameterAlreadyDeclared.GetDiagnostic("a"),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Function_Must_Have_Name()
        {
            var text = @"
                function [(]a: int, b: int) : int
                {
                    return a + b
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.OpenParenthesisToken, SyntaxKind.IdentifierToken),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Bad_Type()
        {
            var text = @"
                function test(n: [invalidtype])
                {
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedType.GetDiagnostic("invalidtype"),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_CallExpression_WrongArgumentType()
        {
            var text = @"
                    function a(x : int)
                    {

                    }
                    a([""42""])
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotConvertImplicitly.GetDiagnostic(TypeSymbol.String, TypeSymbol.Int),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_InvokeFunctionArguments_Missing()
        {
            var text = @"
                print([)]
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.WrongArgumentCount.GetDiagnostic("print", 1, 0),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_CallExpression_WrongArgumentCount()
        {
            var text = @"
                {
                    print(0[, 100, 100])
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.WrongArgumentCount.GetDiagnostic("print", 1, 3),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_CallExpression_Reports_Undefined()
        {
            var text = @"[foo](42)";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedFunction.GetDiagnostic("foo"),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_CallExpression_Reports_NotAFunction()
        {
            var text = @"
                {
                    var foo = 42
                    [foo](42)
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.NotAFunction.GetDiagnostic("foo"),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Variables_Can_Shadow_Functions()
        {
            var text = @"
                {
                    const print = 42
                    [print](""test"")
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.NotAFunction.GetDiagnostic("print"),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_InvokeFunctionArguments_NoInfiniteLoop()
        {
            var text = @"
                print(""Hi""[[=]][)]
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EqualsToken, SyntaxKind.CloseParenthesisToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EqualsToken, SyntaxKind.IdentifierToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.CloseParenthesisToken, SyntaxKind.IdentifierToken),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_FunctionParameters_NoInfiniteLoop()
        {
            var text = @"
                function hi(name: string[[[=]]][)]
                {
                    print(""Hi "" + name + ""!"" )
                }[]
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EqualsToken, SyntaxKind.CloseParenthesisToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EqualsToken, SyntaxKind.OpenBraceToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EqualsToken, SyntaxKind.IdentifierToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.CloseParenthesisToken, SyntaxKind.IdentifierToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EndOfFileToken, SyntaxKind.CloseBraceToken),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_MultiLineComment_ReportsUnterminated()
        {
            var text = @"[/*] unterminated comment";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnterminatedMultilineComment.GetDiagnostic(),
            };

            AssertHasDiagnostics(text, diagnostics);
        }

        private static void AssertValue(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = Compilation.CreateScript(null, syntaxTree);

            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }

        private void AssertHasDiagnostics(string text, List<string> expectedDiagnostics)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);

            var compilation = Compilation.CreateScript(null, syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            if (annotatedText.Spans.Length != result.Diagnostics.Length)
            {
                throw new InvalidOperationException("ERROR: Must mark the same number os spans as there are expected diagnostics");
            }

            Assert.Equal(expectedDiagnostics.Count, result.Diagnostics.Length);
            for (var i = 0; i < result.Diagnostics.Length; i++)
            {
                Assert.Equal(annotatedText.Spans[i], result.Diagnostics[i].Location.Span);
                Assert.Equal(expectedDiagnostics[i], result.Diagnostics[i].Message);
            }
        }
    }
}
