using System;
using System.Collections.Generic;
using Compiler.CodeAnalysis;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Compiler.Tests.CodeAnalysis.Binding
{
    public class BinderTests
    {
        private readonly ITestOutputHelper _output;

        public BinderTests(ITestOutputHelper output)
        {
            _output = output;
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
            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_DoWhileStatement_Reports_CannotConvert()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_WhileStatement_Reports_CannotConvert()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_ForStatement_Reports_CannotConvert_LowerBound()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_ForStatement_Reports_CannotConvert_UpperBound()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_ForStatement_Reports_CannotConvert_Step()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_BreakOrContinueStatement_Reports_InvalidBreakOrContinue()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_AssignmentExpression_Reports_Undefined()
        {
            var text = @"[x] = 1";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedName.GetDiagnostic("x")
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_AssignmentExpression_Reports_CannotReassign()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_AssignmentExpression_Reports_CannotConvert_Implicitly()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_AssignmentExpression_Reports_CannotConvert_Explicit()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_AssignmentExpression_Reports_NotAVariable()
        {
            var text = @"[print] = 42";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.NotAVariable.GetDiagnostic("print")
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_AssignmentExpression_SupportsDefault()
        {
            var text = @"
            {
                var x : int
                var y : int = default
                x = y
            }";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_MemberAccess()
        {
            var text = @"
                enum A { A1, A2, A3 }
                print(A.A1)
            ";
            var diagnostics = new List<string>()
            {
            };

            var compilation = AssertDiagnostics(text, diagnostics);
            var enumSymbol = Assert.Single(compilation.Enums);
            Assert.Equal("A", enumSymbol.Name);
            Assert.Equal(3, enumSymbol.Members.Length);
            Assert.True(enumSymbol.IsEnum());
        }

        [Fact]
        public void Binder_NameExpression_Reports_Undefined()
        {
            var text = "[x] * 1";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedName.GetDiagnostic("x")
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_UnaryExpression_Reports_UndefinedOperator()
        {
            var text = "[+]false";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedUnaryOperator.GetDiagnostic("+", TypeSymbol.Bool)
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_BinaryExpression_Reports_UndefinedOperator()
        {
            var text = "10 [+] false";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic("+", TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_CompoundExpression_Reports_Undefined()
        {
            var text = @"var x = 10
                         x [+=] false";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic("+=", TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_CompoundExpression_Assignemnt_NonDefinedVariable_Reports_Undefined()
        {
            var text = @"[x] += 10";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedName.GetDiagnostic("x")
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_CompoundDeclarationExpression_Reports_CannotAssign()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_VariableDeclaration_Reports_Redeclaration()
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
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_NameExpression_Reports_NoErrorForInsertedToken()
        {
            var text = @"1 + []";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EndOfFileToken, SyntaxKind.IdentifierToken)
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_BlockStatement_NoInfiniteLoop()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_Void_Function_Should_Not_Return_Value()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_Function_With_ReturnValue_Should_Not_Return_Void()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_FunctionReturn_Missing()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_Parameter_Already_Declared()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_Function_Must_Have_Name()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_Bad_Type()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_CallExpression_WrongArgumentType()
        {
            var text = @"
                    function a(x : int)
                    {

                    }
                    [a]([""42""])
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedOverloadForArguments.GetDiagnostic("a", "(string)"),
                DiagnosticCode.CannotConvertImplicitly.GetDiagnostic(TypeSymbol.String, TypeSymbol.Int),
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_InvokeFunctionArguments_Missing()
        {
            var text = @"
                print([)]
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.WrongArgumentCount.GetDiagnostic("print", 1, 0),
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_CallExpression_WrongArgumentCount()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_CallExpression_Reports_Undefined()
        {
            var text = @"[foo](42)";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedFunction.GetDiagnostic("foo"),
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_CallExpression_Reports_NotAFunction()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_Variables_Can_Shadow_Functions()
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

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_InvokeFunctionArguments_NoInfiniteLoop()
        {
            var text = @"
                print(""Hi""=[)]
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.CloseParenthesisToken, SyntaxKind.IdentifierToken),
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_FunctionParameters_NoInfiniteLoop()
        {
            var text = @"
                function hi(name: string[[[=]]][[)]]
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
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.CloseParenthesisToken, SyntaxKind.IdentifierToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EndOfFileToken, SyntaxKind.CloseBraceToken),
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_MultiLineComment_ReportsUnterminated()
        {
            var text = @"[/*] unterminated comment";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnterminatedMultilineComment.GetDiagnostic(),
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_Cannot_Access_Member()
        {
            const string? text = @"
                var p: int = 0
                p.[length]
                p.[length]()
            ";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotAccessMember.GetDiagnostic("length", "int"),
                DiagnosticCode.UndefinedFunction.GetDiagnostic("length", "int"),
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_MethodDeclaration_SupportsOverloading()
        {
            var text = @"
                    function a(x : int)
                    {

                    }

                    function a(x : string)
                    {

                    }

                    a(42)
                    a(""42"")
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_MethodDeclaration_ReportAlreadyDeclared()
        {
            var text = @"
                    function a(x : int)
                    {

                    }

                    function [a](x : int)
                    {

                    }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.SymbolAlreadyDeclared.GetDiagnostic("a")
            };

            AssertDiagnostics(text, diagnostics);
        }
        

        [Fact]
        public void Binder_StructDeclaration()
        {
            var text = @"
                    struct Test
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    var test : Test
            ";

            var diagnostics = new List<string>()
            {
            };

            var compilation = AssertDiagnostics(text, diagnostics);
            var structSymbol = Assert.Single(compilation.Structs);
            Assert.Equal("Test", structSymbol.Name);
            Assert.Equal(3, structSymbol.CtorParameters.Length);
            Assert.Equal(TypeSymbol.Int, structSymbol.CtorParameters[0].Type);
            Assert.Equal(TypeSymbol.Bool, structSymbol.CtorParameters[1].Type);
            Assert.Equal(TypeSymbol.String, structSymbol.CtorParameters[2].Type);
            Assert.True(structSymbol.IsValueType());
        }
        
        [Fact]
        public void Binder_MemberAssignment()
        {
            var text = @"
                    struct Test
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    var test : Test
                    test.a = 100
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_MemberAccess_Nested()
        {
            var text = @"
                    struct Point
                    {
                        var x = 0
                        var y: int
                    }
                    
                    struct Line
                    {
                        var start: Point
                        var end: Point
                    }

                    var nested = Line(Point(10, 10), Point(5, 5))
                    var x = nested.start.x
                    var y = nested.end.x
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_CompoundMemberAssignment()
        {
            var text = @"
                    struct Test
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    var test : Test
                    test.a += 100
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_MemberAccess_CallExpression()
        {
            var text = @"
                    struct TestStruct
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    function TestStruct.f(i : int)
                    {
                        print(""i"")
                    }

                    var test : TestStruct
                    test.f(10)
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        
        [Fact]
        public void Binder_MemberAccess_CallExpressionWithReceiver()
        {
            var text = @"
                    struct TestStruct
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    function TestStruct.f(i : int)
                    {
                        self.printI()
                    }
                    
                    function TestStruct.printI()
                    {
                        print(""i"")
                    }

                    var test : TestStruct
                    test.f(10)
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_MemberAccess_AccessMemberWithoutSelf()
        {
            var text = @"
                    struct TestStruct
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    function TestStruct.f(i : int)
                    {
                        print(self.a + 1)
                        print(i)
                        print(b)
                    }
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        
        [Fact]
        public void Binder_MemberAccess_Self()
        {
            var text = @"
                    struct TestStruct
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    function TestStruct.f()
                    {
                        print(self.a)
                        print(self.b)
                        printTest(self)
                    }
                    
                    function printTest(t : TestStruct)
                    {
                        print(t.a)
                        print(t.b)
                    }
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        
        [Fact]
        public void Binder_MemberAssignment_CannontAssignFunction()
        {
            var text = @"
                    struct TestStruct
                    {
                        var a : int
                        var b : bool = default
                        var c = ""abc""
                    }

                    function TestStruct.f() : int
                    {
                        return 1
                    }
                    
                    function printTest(t : TestStruct)
                    {
                        t.[f] = 10
                    }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotAccessMember.GetDiagnostic("f", "TestStruct")
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_IfStatement_Reports_UnreachableCode_Warning()
        {
            var text = @"
                function test()
                {
                    const x = 4 * 3
                    if x > 12
                    {
                        [print(""x > 12"")]
                    }
                    else
                    {
                        print(""else"")
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnreachableCode.GetDiagnostic()
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_ElseStatement_Reports_UnreachableCode_Warning()
        {
            var text = @"
                function test(): int
                {
                    if true
                    {
                        return 1
                    }
                    [return 0]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnreachableCode.GetDiagnostic()
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_WhileStatement_Reports_UnreachableCode_Warning()
        {
            var text = @"
                function test()
                {
                    while true
                    {
                        [continue]
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnreachableCode.GetDiagnostic()
            };

            AssertDiagnostics(text, diagnostics);
        }
        

        [Fact]
        public void Binder_InfiniteWhileStatement_Reports_UnreachableCode_Warning()
        {
            var text = @"
                function test() : int
                {
                    while true
                    {
                    }
                    [return 10]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnreachableCode.GetDiagnostic()
            };

            AssertDiagnostics(text, diagnostics);
        }


        private Compilation AssertDiagnostics(string text, List<string> expectedDiagnostics)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);

            var compilation = Compilation.Create(syntaxTree);
            var diagnostic = compilation.Validate();

            if (annotatedText.Spans.Length != diagnostic.Length)
            {
                _output.WriteLine($"Expected:  {annotatedText.Spans.Length}");
                _output.WriteLine($"Actual:  {diagnostic.Length}");

                for (var i = 0; i < diagnostic.Length; i++)
                {
                    var textSpan = syntaxTree.Text.ToString(diagnostic[i].Location.Span);
                    _output.WriteLine($"{diagnostic[i].Message} - {textSpan}");
                }

                throw new InvalidOperationException("ERROR: Must mark the same number os spans as there are expected diagnostics");
            }

            Assert.Equal(expectedDiagnostics.Count, diagnostic.Length);
            for (var i = 0; i < diagnostic.Length; i++)
            {
                Assert.Equal(expectedDiagnostics[i], diagnostic[i].Message);
                Assert.Equal(annotatedText.Spans[i], diagnostic[i].Location.Span);
            }

            return compilation;
        }
    }
}
