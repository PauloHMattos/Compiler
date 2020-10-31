﻿using System;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Diagnostic;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        private readonly string _text;

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
        
        public Lexer(string text)
        {
            _text = text;
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
                case '(':
                    _position++;
                    _kind = SyntaxKind.OpenParenthesisToken;
                    break;
                case ')':
                    _position++;
                    _kind = SyntaxKind.CloseParenthesisToken;
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
                    break;
                case '|':
                    _position++;
                    if (Current == '|')
                    {
                        _position++;
                        _kind = SyntaxKind.PipePipeToken;
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
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    LexDigit();
                    break;
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    LexWhitespace();
                    break;
                default:
                    if (char.IsLetter(Current))
                    {
                        LexLetter();
                    }
                    else if (char.IsWhiteSpace(Current))
                    {
                        LexWhitespace();
                    }
                    else
                    {
                        Diagnostics.ReportBadCharacter(new TextSpan(_position, 1), Current);
                        _kind = SyntaxKind.BadToken;
                        _position++;
                    }
                    break;
            }
            
            _tokenText ??= _kind.GetText();
            if (_tokenText == null)
            {
                var length = _position - _start;
                _tokenText = _text.Substring(_start, length);
            }
            return new SyntaxToken(_kind, _start, _tokenText, _value);
        }

        private void LexDigit()
        {
            ConsumesTokenWhile(char.IsDigit);
            var length = _position - _start;
            _tokenText = _text.Substring(_start, length);
            if (!int.TryParse(_tokenText, out var value))
            {
                Diagnostics.ReportInvalidLiteralType(new TextSpan(_position, length), _tokenText, TypeSymbol.Int);
            }

            _value = value;
            _kind = SyntaxKind.NumberToken;
        }

        private void LexWhitespace()
        {
            ConsumesTokenWhile(char.IsWhiteSpace);
            _kind = SyntaxKind.WhitespaceToken;
        }

        private void LexLetter()
        {
            ConsumesTokenWhile(char.IsLetter);
            var length = _position - _start;
            _tokenText = _text.Substring(_start, length);
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