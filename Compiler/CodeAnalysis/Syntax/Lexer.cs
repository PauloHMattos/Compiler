using System;
using System.Collections.Immutable;
using System.Text;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly ImmutableArray<SyntaxTrivia>.Builder _triviaBuilder;

        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object _value;
        private string _tokenText;

        public DiagnosticBag Diagnostics { get; }

        private char Current => Peek(0);
        private char Lookahead => Peek(1);

        private char Peek(int offset)
        {
            var index = _position + offset;
            if (index >= _text.Length)
            {
                return '\0';
            }
            return _text[index];
        }

        public Lexer(SyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
            _text = _syntaxTree.Text;
            Diagnostics = new DiagnosticBag();
            _triviaBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();
        }

        public SyntaxToken Lex()
        {
            LexTrivia(true);
            var leadingTrivia = _triviaBuilder.ToImmutable();

            var tokenStart = _position;
            LexToken();
            var tokenKind = _kind;
            var tokenValue = _value;
            var tokenLength = _position - tokenStart;

            LexTrivia(false);
            var trailingTrivia = _triviaBuilder.ToImmutable();

            _tokenText ??= tokenKind.GetText();
            if (_tokenText == null)
            {
                _tokenText = _text.ToString(tokenStart, tokenLength);
            }
            return new SyntaxToken(_syntaxTree, tokenKind, tokenStart, _tokenText, tokenValue, leadingTrivia, trailingTrivia);
        }

        public void LexToken()
        {
            _start = _position;
            _kind = SyntaxKind.BadToken;
            _value = null;
            _tokenText = null;

            switch (Current)
            {
                case '\0':
                    _kind = SyntaxKind.EndOfFileToken;
                    break;
                case ',':
                    _position++;
                    _kind = SyntaxKind.CommaToken;
                    break;
                case ':':
                    _kind = SyntaxKind.ColonToken;
                    _position++;
                    break;
                case '+':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.PlusEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.PlusToken;
                    }
                    break;
                case '-':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.MinusEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.MinusToken;
                    }
                    break;
                case '*':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.StarEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.StarToken;
                    }
                    break;
                case '/':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.SlashEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.SlashToken;
                    }
                    break;
                case '%':
                    _position++;
                    _kind = SyntaxKind.PercentToken;
                    break;
                case '(':
                    _position++;
                    _kind = SyntaxKind.OpenParenthesisToken;
                    break;
                case ')':
                    _position++;
                    _kind = SyntaxKind.CloseParenthesisToken;
                    break;
                case '{':
                    _position++;
                    _kind = SyntaxKind.OpenBraceToken;
                    break;
                case '}':
                    _position++;
                    _kind = SyntaxKind.CloseBraceToken;
                    break;
                case '~':
                    _position++;
                    _kind = SyntaxKind.TildeToken;
                    break;
                case '^':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.HatEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.HatToken;
                    }
                    break;
                case '!':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.BangEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.BangToken;
                    }
                    break;
                case '&':
                    _position++;
                    if (Current == '&')
                    {
                        _position++;
                        _kind = SyntaxKind.AmpersandAmpersandToken;
                    }
                    else if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.AmpersandEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.AmpersandToken;
                    }
                    break;
                case '|':
                    _position++;
                    if (Current == '|')
                    {
                        _position++;
                        _kind = SyntaxKind.PipePipeToken;
                    }
                    else if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.PipeEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.PipeToken;
                    }
                    break;
                case '=':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.EqualsEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.EqualsToken;
                    }
                    break;
                case '<':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.LessOrEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.LessToken;
                    }
                    break;
                case '>':
                    _position++;
                    if (Current == '=')
                    {
                        _position++;
                        _kind = SyntaxKind.GreaterOrEqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.GreaterToken;
                    }
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    LexDigit();
                    break;
                case '"':
                    LexString();
                    break;
                case '_':
                    LexIdentifierOrKeyword();
                    break;
                default:
                    if (char.IsLetter(Current))
                    {
                        LexIdentifierOrKeyword();
                    }
                    else
                    {
                        var span = new TextSpan(_position, 1);
                        var location = new TextLocation(_text, span);
                        Diagnostics.ReportBadCharacter(location, Current);
                        _kind = SyntaxKind.BadToken;
                        _position++;
                    }
                    break;
            }
        }

        private void LexString()
        {
            _position++;
            var builder = new StringBuilder();

            var done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\n':
                    case '\r':
                        done = true;
                        var span = new TextSpan(_start, 1);
                        var location = new TextLocation(_text, span);
                        Diagnostics.ReportUnterminatedString(location);
                        break;
                    case '"':
                        _position++;
                        if (Current == '"')
                        {
                            builder.Append(Current);
                            _position++;
                        }
                        else
                        {
                            done = true;
                        }
                        break;
                    default:
                        builder.Append(Current);
                        _position++;
                        break;
                }
            }

            _kind = SyntaxKind.StringToken;
            _value = builder.ToString();
        }

        private void LexDigit()
        {
            ConsumesTokenWhile(char.IsDigit);
            var length = _position - _start;
            _tokenText = _text.ToString(_start, length);
            if (!int.TryParse(_tokenText, out var value))
            {
                var span = new TextSpan(_position, length);
                var location = new TextLocation(_text, span);
                Diagnostics.ReportInvalidLiteralType(location, _tokenText, TypeSymbol.Int);
            }

            _value = value;
            _kind = SyntaxKind.NumberToken;
        }

        private void LexTrivia(bool leading)
        {
            _triviaBuilder.Clear();
            var done = false;
            while (!done)
            {
                _start = _position;
                _kind = SyntaxKind.BadToken;
                _value = null;
                switch (Current)
                {
                    case '\0':
                        done = true;
                        break;

                    case '/':
                        if (Lookahead == '/')
                        {
                            LexSingleLineComment();
                        }
                        else if (Lookahead == '*')
                        {
                            LexMultiLineComment();
                        }
                        else
                        {
                            done = true;
                        }
                        break;

                    case '\n':
                    case '\r':
                        if (!leading)
                        {
                            done = true; 
                        }
                        LexLineBreak();
                        break;

                    case ' ':
                    case '\t':
                        LexWhitespace();
                        break;

                    default:
                        if (char.IsWhiteSpace(Current))
                        {
                            LexWhitespace();
                        }
                        else
                        {
                            done = true;
                        }
                        break;
                }

                var length = _position - _start;
                if (length == 0)
                {
                    continue;
                }
                
                var text = _text.ToString(_start, length);
                var trivia = new SyntaxTrivia(_syntaxTree, _kind, _start, text);
                _triviaBuilder.Add(trivia);
            }

        }

        private void LexWhitespace()
        {
            var done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        done = true;
                        break;
                    default:
                        if (!char.IsWhiteSpace(Current))
                            done = true;
                        else
                            _position++;
                        break;
                }
            }

            _kind = SyntaxKind.WhitespaceTrivia;
        }

        private void LexLineBreak()
        {
            if (Current == '\r' && Lookahead == '\n')
            {
                _position += 2;
            }
            else
            {
                _position++;
            }

            _kind = SyntaxKind.LineBreakTrivia;
        }

        private void LexSingleLineComment()
        {
            _position++;
            ConsumesTokenWhile(c => c != '\n' && c != '\r' && c != '\0');
            _kind = SyntaxKind.SingleLineCommentTrivia;
        }

        private void LexMultiLineComment()
        {
            _position++;

            var done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                        done = true;
                        var span = new TextSpan(_start, 2);
                        var location = new TextLocation(_text, span);
                        Diagnostics.ReportUnterminatedMultilineComment(location);
                        break;
                    case '*':
                        _position++;
                        if (Current == '/')
                        {
                            _position++;
                            done = true;
                        }
                        break;
                    default:
                        _position++;
                        break;
                }
            }
            _kind = SyntaxKind.MultiLineCommentTrivia;
        }

        private void LexIdentifierOrKeyword()
        {
            ConsumesTokenWhile(c => char.IsLetterOrDigit(c) || c == '_');
            var length = _position - _start;
            _tokenText = _text.ToString(_start, length);
            _kind = SyntaxFacts.GetKeywordKind(_tokenText);
        }

        private void ConsumesTokenWhile(Func<char, bool> func)
        {
            while (func(Current))
            {
                _position++;
            }
        }
    }
}