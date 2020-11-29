using System;
using System.Collections.Generic;

namespace Compiler.CodeAnalysis.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.TildeToken:
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.BangToken:
                    return 7;

                default:
                    return 0;
            }
        }

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.PercentToken:
                    return 6;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 5;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                case SyntaxKind.LessToken:
                case SyntaxKind.LessOrEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterOrEqualsToken:
                    return 4;

                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AmpersandAmpersandToken:
                    return 3;

                case SyntaxKind.HatToken:
                case SyntaxKind.PipeToken:
                case SyntaxKind.PipePipeToken:
                    return 2;

                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.StarEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.PipeEqualsToken:
                case SyntaxKind.HatEqualsToken:
                case SyntaxKind.EqualsToken:
                    return 1;


                default:
                    return 0;
            }
        }

        public static SyntaxKind GetBinaryOperatorOfAssignmentOperator(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusEqualsToken:
                    return SyntaxKind.PlusToken;
                case SyntaxKind.MinusEqualsToken:
                    return SyntaxKind.MinusToken;
                case SyntaxKind.StarEqualsToken:
                    return SyntaxKind.StarToken;
                case SyntaxKind.SlashEqualsToken:
                    return SyntaxKind.SlashToken;
                case SyntaxKind.AmpersandEqualsToken:
                    return SyntaxKind.AmpersandToken;
                case SyntaxKind.PipeEqualsToken:
                    return SyntaxKind.PipeToken;
                case SyntaxKind.HatEqualsToken:
                    return SyntaxKind.HatToken;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), $"Unexpected syntax: '{kind}'");
            }
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            switch (text)
            {
                case "true":
                    return SyntaxKind.TrueKeyword;
                case "false":
                    return SyntaxKind.FalseKeyword;
                case "const":
                    return SyntaxKind.ConstKeyword;
                case "var":
                    return SyntaxKind.VarKeyword;
                case "if":
                    return SyntaxKind.IfKeyword;
                case "else":
                    return SyntaxKind.ElseKeyword;
                case "do":
                    return SyntaxKind.DoKeyword;
                case "while":
                    return SyntaxKind.WhileKeyword;
                case "for":
                    return SyntaxKind.ForKeyword;
                case "to":
                    return SyntaxKind.ToKeyword;
                case "step":
                    return SyntaxKind.StepKeyword;
                case "continue":
                    return SyntaxKind.ContinueKeyword;
                case "break":
                    return SyntaxKind.BreakKeyword;
                case "function":
                    return SyntaxKind.FunctionKeyword;
                case "return":
                    return SyntaxKind.ReturnKeyword;
                case "default":
                    return SyntaxKind.DefaultKeyword;
                case "enum":
                    return SyntaxKind.EnumKeyword;
                case "struct":
                    return SyntaxKind.StructKeyword;
                default:
                    return SyntaxKind.IdentifierToken;
            }
        }

        public static string? GetText(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.DotToken:
                    return ".";
                case SyntaxKind.CommaToken:
                    return ",";
                case SyntaxKind.ColonToken:
                    return ":";
                case SyntaxKind.OpenParenthesisToken:
                    return "(";
                case SyntaxKind.CloseParenthesisToken:
                    return ")";
                case SyntaxKind.OpenBraceToken:
                    return "{";
                case SyntaxKind.CloseBraceToken:
                    return "}";
                case SyntaxKind.FalseKeyword:
                    return "false";
                case SyntaxKind.TrueKeyword:
                    return "true";
                case SyntaxKind.ConstKeyword:
                    return "const";
                case SyntaxKind.VarKeyword:
                    return "var";
                case SyntaxKind.IfKeyword:
                    return "if";
                case SyntaxKind.ElseKeyword:
                    return "else";
                case SyntaxKind.DoKeyword:
                    return "do";
                case SyntaxKind.WhileKeyword:
                    return "while";
                case SyntaxKind.ForKeyword:
                    return "for";
                case SyntaxKind.ToKeyword:
                    return "to";
                case SyntaxKind.StepKeyword:
                    return "step";
                case SyntaxKind.ContinueKeyword:
                    return "continue";
                case SyntaxKind.BreakKeyword:
                    return "break";
                case SyntaxKind.FunctionKeyword:
                    return "function";
                case SyntaxKind.ReturnKeyword:
                    return "return";
                case SyntaxKind.DefaultKeyword:
                    return "default";
                case SyntaxKind.EnumKeyword:
                    return "enum";
                case SyntaxKind.StructKeyword:
                    return "struct";
                case SyntaxKind.PlusToken:
                    return "+";
                case SyntaxKind.PlusEqualsToken:
                    return "+=";
                case SyntaxKind.MinusToken:
                    return "-";
                case SyntaxKind.MinusEqualsToken:
                    return "-=";
                case SyntaxKind.StarToken:
                    return "*";
                case SyntaxKind.StarEqualsToken:
                    return "*=";
                case SyntaxKind.SlashToken:
                    return "/";
                case SyntaxKind.SlashEqualsToken:
                    return "/=";
                case SyntaxKind.PercentToken:
                    return "%";
                case SyntaxKind.TildeToken:
                    return "~";
                case SyntaxKind.HatToken:
                    return "^";
                case SyntaxKind.HatEqualsToken:
                    return "^=";
                case SyntaxKind.BangToken:
                    return "!";
                case SyntaxKind.AmpersandToken:
                    return "&";
                case SyntaxKind.AmpersandEqualsToken:
                    return "&=";
                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxKind.PipeToken:
                    return "|";
                case SyntaxKind.PipeEqualsToken:
                    return "|=";
                case SyntaxKind.PipePipeToken:
                    return "||";
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.BangEqualsToken:
                    return "!=";
                case SyntaxKind.EqualsEqualsToken:
                    return "==";
                case SyntaxKind.LessToken:
                    return "<";
                case SyntaxKind.LessOrEqualsToken:
                    return "<=";
                case SyntaxKind.GreaterToken:
                    return ">";
                case SyntaxKind.GreaterOrEqualsToken:
                    return ">=";
                default:
                    return null;
            }
        }

        public static IEnumerable<SyntaxKind> GetUnaryOperatorKinds()
        {
            var kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (var kind in kinds)
            {
                if (GetUnaryOperatorPrecedence(kind) > 0)
                {
                    yield return kind;
                }
            }
        }

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds()
        {
            var kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (var kind in kinds)
            {
                if (GetBinaryOperatorPrecedence(kind) > 0)
                {
                    yield return kind;
                }
            }
        }

        public static bool IsKeyword(this SyntaxKind kind)
        {
            return kind.ToString().EndsWith("Keyword");
        }

        public static bool IsToken(this SyntaxKind kind)
        {
            return !kind.IsTrivia() &&
                    kind.IsKeyword() ||
                    kind.ToString().EndsWith("Token");
        }

        public static bool IsComment(this SyntaxKind kind)
        {
            return kind == SyntaxKind.SingleLineCommentTrivia ||
                   kind == SyntaxKind.MultiLineCommentTrivia;
        }

        public static bool IsTrivia(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.SkippedTextTrivia:
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.LineBreakTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsWhitespace(this SyntaxKind kind)
        {
            return kind == SyntaxKind.WhitespaceTrivia ||
                   kind == SyntaxKind.LineBreakTrivia;
        }

        public static bool IsAssignmentOperator(this SyntaxKind kind)
        {
            return kind == SyntaxKind.PlusEqualsToken
                || kind == SyntaxKind.MinusEqualsToken
                || kind == SyntaxKind.StarEqualsToken
                || kind == SyntaxKind.SlashEqualsToken
                || kind == SyntaxKind.AmpersandEqualsToken
                || kind == SyntaxKind.PipeEqualsToken
                || kind == SyntaxKind.HatEqualsToken
                || kind == SyntaxKind.EqualsToken;
        }
    }
}
