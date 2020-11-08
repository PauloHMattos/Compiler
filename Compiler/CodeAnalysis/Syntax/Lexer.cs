using System;
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

        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object _value;
        private string _tokenText;

        public DiagnosticBag Diagnostics { get; }

        private char Current 
        {
            get
            {
                if (_position >= _text.Length)
                {
                    return '\0';
                }
                return _text[_position];
            }    
        }
        
        public Lexer(SyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
            _text = _syntaxTree.Text;
            Diagnostics = new DiagnosticBag();
        }

        public SyntaxToken Lex()
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
                    _kind = SyntaxKind.PlusToken;
                    break;
                case '-':
                    _position++;
                    _kind = SyntaxKind.MinusToken;
                    break;
                case '*':
                    _position++;
                    _kind = SyntaxKind.StarToken;
                    break;
                case '/':
                    _position++;
                    _kind = SyntaxKind.SlashToken;
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
                    _kind = SyntaxKind.HatToken;
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
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    LexDigit();
                    break;
                case '"':
                    LexString();
                    break;
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    LexWhitespace();
                    break;
                case '_':
                    LexIdentifierOrKeyword();
                    break;
                default:
                    if (char.IsLetter(Current))
                    {
                        LexIdentifierOrKeyword();
                    }
                    else if (char.IsWhiteSpace(Current))
                    {
                        LexWhitespace();
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
            
            _tokenText ??= _kind.GetText();
            if (_tokenText == null)
            {
                var length = _position - _start;
                _tokenText = _text.ToString(_start, length);
            }
            return new SyntaxToken(_syntaxTree, _kind, _start, _tokenText, _value);
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

        private void LexWhitespace()
        {
            ConsumesTokenWhile(char.IsWhiteSpace);
            _kind = SyntaxKind.WhitespaceToken;
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