using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        private int _position;
        private readonly string _text;

        public DiagnosticBag Diagnostics { get; }

        private char Current => Peek(0);
        private char Lookahead => Peek(1);

        public Lexer(string text)
        {
            _text = text;
            Diagnostics = new DiagnosticBag();
        }

        private char Peek(int offset)
        {
            var index = _position + offset;
                if (index >= _text.Length)
                {
                    return '\0';
                }
                return _text[index];
        }

        private void Next()
        {
            _position++;
        }

        public SyntaxToken Lex()
        {
            if (_position >= _text.Length)
            {
                return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);
            }

            if (char.IsDigit(Current))
            {
                return LexDigit();
            }

            if (char.IsWhiteSpace(Current))
            {
                return LexWhitespace();
            }

            if (char.IsLetter(Current))
            {
                return LexLetter();
            }

            var start = _position;
            switch (Current)
            {
                case '+':
                    return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
                case '-':
                    return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
                case '*':
                    return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
                case '/':
                    return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
                case '(':
                    return new SyntaxToken(SyntaxKind.OpenParenthesisToken, _position++, "(", null);
                case ')':
                    return new SyntaxToken(SyntaxKind.CloseParenthesisToken, _position++, ")", null);
                case '!':
                    if (Lookahead == '=')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.BangEqualsToken, start, "!=", null);
                    }
                    return new SyntaxToken(SyntaxKind.BangToken, _position++, "!", null);
                case '&':
                    if (Lookahead == '&')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.AmpersandAmpersandToken, start, "&&", null);
                    }
                    break;
                case '|':
                    if (Lookahead == '|')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.PipePipeToken, start, "||", null);
                    }
                    break;
                case '=':
                    if (Lookahead == '=')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.EqualsEqualsToken, start, "==", null);
                    }
                    return new SyntaxToken(SyntaxKind.EqualsToken, _position++, "=", null);
            }

            Diagnostics.ReportBadCharacter(new TextSpan(_position, 1), Current);
            return new SyntaxToken(SyntaxKind.BadToken, _position, _text.Substring(_position++, 1), null);
        }
        private SyntaxToken LexDigit()
        {
            var start = _position;
            while (char.IsDigit(Current))
            {
                Next();
            }

            var length = _position - start;
            var text = _text.Substring(start, length);
            if (!int.TryParse(text, out var value))
            {
                Diagnostics.ReportInvalidLiteralType(new TextSpan(_position, length), text, TypeSymbol.Int);
            }

            return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
        }
        private SyntaxToken LexWhitespace()
        {
            var start = _position;
            while (char.IsWhiteSpace(Current))
            {
                Next();
            }

            var length = _position - start;
            var text = _text.Substring(start, length);
            return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text, null);
        }

        private SyntaxToken LexLetter()
        {
            var start = _position;
            while (char.IsLetter(Current))
            {
                Next();
            }

            var length = _position - start;
            var text = _text.Substring(start, length);
            var kind = SyntaxFacts.GetKeywordKind(text);
            return new SyntaxToken(kind, start, text, null);
        }
    }
}