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

        private static void Main()
        {
            var variables = new Dictionary<VariableSymbol, object>();
            var textBuilder = new StringBuilder();

            while (true)
            {
                if (!Loop(textBuilder, variables))
                {
                    break;
                }
            }
        }

        private static bool Loop(StringBuilder textBuilder, Dictionary<VariableSymbol, object> variables)
        {
            Console.Write(textBuilder.Length == 0 ? "> " : "| ");

            var input = Console.ReadLine();
            var isBlank = string.IsNullOrWhiteSpace(input);

            if (textBuilder.Length == 0)
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

            textBuilder.AppendLine(input);
            var text = textBuilder.ToString();

            var syntaxTree = SyntaxTree.Parse(text);

            if (!isBlank && syntaxTree.Diagnostics.Any())
            {
                return true;
            }

            var compilation = new Compilation(syntaxTree);
            var compilationResult = compilation.Evaluate(variables);


            if (_showTree)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                syntaxTree.Root.WriteTo(Console.Out);
                Console.ResetColor();
            }

            var diagnostics = compilationResult.Diagnostics;
            if (!diagnostics.Any())
            {
                Console.WriteLine(compilationResult.Value);
            }
            else
            {
                PrintDiagnostics(syntaxTree.Text, diagnostics);
            }

            textBuilder.Clear();
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

                default:
                    return false;
            }
        }
    }
}