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
        public void Binder_GlobalStatement_NotAllowed()
        {
            var text = @"
                [var x = 0]
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.InvalidGlobalStatement.GetDiagnostic()
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_IfStatement_ExpressionInCondition()
        {
            var text = @"
                function main()
                {
                    var x = 0
                    if x + 10 > 5
                    {
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_IfStatement_WithElse()
        {
            var text = @"
                function main()
                {
                    var x = 0
                    if x + 10 > 5
                    {
                    }
                    else
                    {
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_IfStatement_WithElseIf()
        {
            var text = @"
                function main()
                {
                    var x = 0
                    if x + 10 > 15
                    {
                    }
                    else if (x + 10 > 5)
                    {
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_WhileStatement_ExpressionInCondition()
        {
            var text = @"
                function main()
                {
                    var x = 10
                    while (x + 10 > 0)
                    {
                        x -= 1
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics, true);
        }

        
        [Fact]
        public void Binder_WhileStatement_Break()
        {
            var text = @"
                function main()
                {
                    var x = 10
                    while (x + 10 > 0)
                    {
                        break
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics, true);
        }
        
        [Fact]
        public void Binder_NestedLoop_BreakContinue()
        {
            var text = @"
                function main()
                {
                    for x = 0 to 10
                    {
                        while (x + 10 > 0)
                        {
                            break
                        }
                        continue
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics, true);
        }

        [Fact]
        public void Binder_IfStatement_Reports_CannotConvert()
        {
            var text = @"
                function main()
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
                function main()
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
                function main()
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
                function main()
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
                function main()
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
                function main()
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
                function main()
                {
                    [break]
                    [continue]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.InvalidBreakOrContinue.GetDiagnostic("break"),
                DiagnosticCode.InvalidBreakOrContinue.GetDiagnostic("continue"),
            };
            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_AssignmentExpression_Reports_Undefined()
        {
            var text = @"
                function main()
                {
                    [x] = 1
                }";

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
                function main()
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
                function main()
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
                function main()
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
            var text = @"
                function main()
                {
                    [print] = 42
                }";

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
                function main()
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
        public void Binder_AssignmentExpression_DefaultsNoTypeClause_Reports()
        {
            var text = @"
                function main()
                {
                    var x : int
                    var y = [default]
                }";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.TypeNotFoundForDefault.GetDiagnostic()
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_EnumDeclaration()
        {
            var text = @"
                enum A { A1, A2, A3 }
                ";

            var diagnostics = new List<string>()
            {
            };

            var compilation = AssertDiagnostics(text, diagnostics);
            var enumSymbol = Assert.Single(compilation.Types.OfType<EnumSymbol>());
            Assert.Equal("A", enumSymbol.Name);
            Assert.Equal(3, enumSymbol.Members.Length);
            Assert.True(enumSymbol.IsEnum());
        }
        
        [Fact]
        public void Binder_EnumDeclaration_ReportsRepeatedValue()
        {
            var text = @"
                enum A { A1, [A2 = 0], A3 }
                ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.EnumerationAlreadyContainsValue.GetDiagnostic("A", 0, "A1")
            };

            var compilation = AssertDiagnostics(text, diagnostics);
            var enumSymbol = Assert.Single(compilation.Types.OfType<EnumSymbol>());
            Assert.Equal(3, enumSymbol.Members.Length);
        }
        
        [Fact]
        public void Binder_Enum_MemberAccess()
        {
            var text = @"
                enum A { A1, A2, A3 }
                
                function main()
                {
                    print(A.A1)
                }";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_NameExpression_Reports_Undefined()
        {
            var text = @"
                function main()
                {
                    [x] * 1
                }";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedName.GetDiagnostic("x")
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_UnaryExpression_Reports_UndefinedOperator()
        {
            var text = @"
                function main()
                {
                    [+]false
                }";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedUnaryOperator.GetDiagnostic("+", TypeSymbol.Bool)
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_BinaryExpression_Reports_UndefinedOperator()
        {
            var text = @"
                function main()
                {
                    10 [+] false
                }";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic("+", TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_CompoundExpression_Reports_Undefined()
        {
            var text = @"
                function main()
                {
                    var x = 10
                    x [+=] false
                }";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedBinaryOperator.GetDiagnostic("+=", TypeSymbol.Int, TypeSymbol.Bool)
            };
            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_CompoundExpression_Assignemnt_NonDefinedVariable_Reports_Undefined()
        {
            var text = @"
                function main()
                {
                    [x] += 10
                }";

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
                function main()
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
                function main()
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
        public void Binder_VariableDeclaration_Reports_UnexpectedToken()
        {
            var text = @"
                function main()
                {
                    var [=] 10
                    var [:] int = 10
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.EqualsToken, SyntaxKind.IdentifierToken),
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.ColonToken, SyntaxKind.IdentifierToken)
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_NameExpression_Reports_NoErrorForInsertedToken()
        {
            var text = @"
                function main()
                {
                    1 + 
                [}]";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.UnexpectedToken.GetDiagnostic(SyntaxKind.CloseBraceToken, SyntaxKind.IdentifierToken)
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_BlockStatement_NoInfiniteLoop()
        {
            var text = @"
                function a()
                {
                    {
                    [)]
                }[]
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

                function main()
                {
                    [a]([""42""])
                }
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
                function main()
                {
                    print([)]
                }
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
                function main()
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
            var text = @"
                function main()
                {
                    [foo](42)
                }";

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
                function main()
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
                function main()
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
                function main()
                {
                    print(""Hi""=[)]
                }
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
                function main()
                {
                    var p: int = 0
                    p.[length] = 10
                    p.[length]()
                }
            ";
            var diagnostics = new List<string>()
            {
                DiagnosticCode.UndefinedName.GetDiagnostic("length"),
                DiagnosticCode.UndefinedFunction.GetDiagnostic("length"),
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

                function main()
                {
                    a(42)
                    a(""42"")
                }
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

                function main()
                {
                    var test : Test
                }
            ";

            var diagnostics = new List<string>()
            {
            };

            var compilation = AssertDiagnostics(text, diagnostics);
            var structSymbol = Assert.Single(compilation.Types.OfType<StructSymbol>());
            Assert.Equal("Test", structSymbol.Name);
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

                function main()
                {
                    var test : Test
                    test.a = 100
                }
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_MemberAssignment_ReadOnlyField_Reports_CannotReassign()
        {
            var text = @"
                struct Test
                {
                    const a : int = 10
                }

                function main()
                {
                    var test : Test
                    test.a [=] 100
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.VariableCannotReassigned.GetDiagnostic("a")
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

                function main()
                {
                    var nested = Line()
                    var x = nested.start.x
                    var y = nested.end.x
                }
            ";

            var diagnostics = new List<string>()
            {
            };
            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_MemberAccess_NestedCall()
        {
            var text = @"
                struct Point
                {
                    var x = 0
                    var y: int

                    
                    function B()
                    {
                        
                    }
                }

                struct Line
                {
                    var start: Point
                    var end: Point
                    
                    function A()
                    {
                        start.B()
                    }
                }

                function main()
                {
                    var nested = Line()
                    nested.A()
                }
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

                function main()
                {
                    var test : Test
                    test.a += 100
                }
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

                    function f(i : int)
                    {
                        print(""i"")
                    }
                }


                function main()
                {
                    var test : TestStruct
                    test.f(10)
                }
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

                    function f(i : int)
                    {
                        self.printI()
                    }
                    
                    function printI()
                    {
                        print(""i"")
                    }
                }

                function main()
                {
                    var test : TestStruct
                    test.f(10)
                }
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

                    function f(i : int)
                    {
                        print(b)
                        print(self.a + 1)
                        print(i)
                    }
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

                    function f()
                    {
                        print(self.a)
                        print(self.b)
                    }
                }
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_SelfExpression_Reports_CannotUseSelfOutsideOfReceiverFunctions()
        {
            var text = @"
                function main()
                {
                    print([self])
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.CannotUseSelfOutsideOfReceiverFunctions.GetDiagnostic("main")
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

                    function f() : int
                    {
                        return 1
                    }
                }

                function printTest(t : TestStruct)
                {
                    t.[f] = 10
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.NotAVariable.GetDiagnostic("f")
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_TypeDeclaration_MemberAlreadyDeclared()
        {
            var text = @"
                struct TestStruct
                {
                    var a : int
                    var [a] : bool = default

                    function [a]()
                    {
                    }

                    function b()
                    {
                    }
                    function [b]()
                    {
                    }
                }

                enum TestEnum
                {
                    A,
                    B,
                    [A]
                }
            ";

            var diagnostics = new List<string>()
            {
                DiagnosticCode.AlreadyDeclaredMember.GetDiagnostic("TestStruct", "a"),
                DiagnosticCode.AlreadyDeclaredMember.GetDiagnostic("TestStruct", "a"),
                DiagnosticCode.AlreadyDeclaredMember.GetDiagnostic("TestStruct", "b"),
                DiagnosticCode.AlreadyDeclaredMember.GetDiagnostic("TestEnum", "A")
            };

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Binder_TypeDeclaration_NestedType()
        {
            var text = @"
                struct Line
                {
                    var p1 : Point
                    var p2 : Point
                    
                    struct Point
                    {
                        var x : int
                        var y : int
                    }
                }";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }
        
        [Fact]
        public void Binder_VariableDeclaration_NestedType()
        {
            var text = @"
                struct Line
                {
                    var p1 : Point
                    var p2 : Point
                    
                    struct Point
                    {
                        var x : int
                        var y : int

                        struct Point1
                        {
                        }
                    }
                }
                
                function main()
                {
                    var p = Line.Point.Point1()
                }";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }

        /*
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

            AssertDiagnostics(text, diagnostics, true);
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

            AssertDiagnostics(text, diagnostics, true);
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

            AssertDiagnostics(text, diagnostics, true);
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

            AssertDiagnostics(text, diagnostics, true);
        }
        //*/

        
        [Fact]
        public void Binder_TypeDeclaration_SupportsOverloading()
        {
            var text = @"
                struct TestStruct
                {
                    function a(x : int)
                    {
                        a(string(x))
                    }

                    function a(x : string)
                    {

                    }
                }

                function main()
                {
                    var test : TestStruct
                    test.a(42)
                    test.a(""42"")
                }
            ";

            var diagnostics = new List<string>()
            {
            };

            AssertDiagnostics(text, diagnostics);
        }

        private Compilation AssertDiagnostics(string text, List<string> expectedDiagnostics, bool generateGraph = false)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);

            var compilation = Compilation.Create(syntaxTree);
            var diagnostic = compilation.Validate(generateGraph);

            annotatedText.AssertDiagnostics(_output, expectedDiagnostics, diagnostic);

            return compilation;
        }
    }
}
