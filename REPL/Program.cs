using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Compiler.CodeAnalysis;
using Compiler.CodeAnalysis.Diagnostic;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;

namespace Compiler.REPL
{
    internal static class Program
    {
        private static bool _showTree;
        private static bool _showProgram;
        private static Compilation _previous;
        private static StringBuilder _textBuilder;
        private static Dictionary<VariableSymbol, object> _variables;

        private static void Main()
        {
            _textBuilder = new StringBuilder();
            _variables = new Dictionary<VariableSymbol, object>();
            while (true)
            {
                if (!Loop())
                {
                    break;
                }
            }
        }

        private static bool Loop()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(_textBuilder.Length == 0 ? "» " : "· ");
            Console.ResetColor();

            var input = Console.ReadLine();
            var isBlank = string.IsNullOrWhiteSpace(input);

            if (_textBuilder.Length == 0)
            {
                if (isBlank)
                {
                    return false;
                }

                if (CheckCommands(input))
                {
                    return true;
                }
            }

            _textBuilder.AppendLine(input);
            var text = _textBuilder.ToString();

            var syntaxTree = SyntaxTree.Parse(text);

            if (!isBlank && syntaxTree.Diagnostics.Any())
            {
                return true;
            }

            var compilation = _previous == null ?
                                    new Compilation(syntaxTree) :
                                    _previous.ContinueWith(syntaxTree);

            if (_showTree)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                syntaxTree.Root.WriteTo(Console.Out);
                Console.ResetColor();
            }

            if (_showProgram)
            {
                compilation.EmitTree(Console.Out);
            }

            var compilationResult = compilation.Evaluate(_variables);

            var diagnostics = compilationResult.Diagnostics;
            if (!diagnostics.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine(compilationResult.Value);
                Console.ResetColor();
                _previous = compilation;
            }
            else
            {
                PrintDiagnostics(syntaxTree.Text, diagnostics);
            }

            _textBuilder.Clear();
            return true;
        }

        private static void PrintDiagnostics(SourceText textSource, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                var lineIndex = textSource.GetLineIndex(diagnostic.Span.Start);
                var diagnosticLine = textSource.Lines[lineIndex];
                var character = diagnostic.Span.Start - diagnosticLine.Start + 1;
                var lineNumber = lineIndex + 1;


                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write($"({lineNumber}, {character}): ");
                Console.WriteLine(diagnostic.Message);
                Console.ResetColor();

                var prefixSpan = TextSpan.FromBounds(diagnosticLine.Start, diagnostic.Span.Start);
                var suffixSpan = TextSpan.FromBounds(diagnostic.Span.End, diagnosticLine.End);

                var prefix = textSource.ToString(prefixSpan);
                var error = textSource.ToString(diagnostic.Span.Start, diagnostic.Span.Length);
                var suffix = textSource.ToString(suffixSpan);

                Console.Write(prefix);

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write(error);
                Console.ResetColor();

                Console.WriteLine(suffix);
            }
            Console.WriteLine();
        }

        private static bool CheckCommands(string line)
        {
            switch (line)
            {
                case "#tree":
                    _showTree = !_showTree;
                    Console.WriteLine(_showTree ? "Showing parse trees" : "Not showing parse trees");
                    return true;

                case "#cls":
                    Console.Clear();
                    return true;

                case "#reset":
                    _previous = null;
                    return true;

                case "#program":
                    _showProgram = !_showProgram;
                    Console.WriteLine(_showProgram ? "Showing bound trees" : "Not showing bound trees");
                    return true;

                default:
                    return false;
            }
        }
    }
}