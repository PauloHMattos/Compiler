using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.CodeAnalysis;
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
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                if (CheckCommands(line))
                {
                    continue;
                }

                var syntaxTree = SyntaxTree.Parse(line);
                var compilation = new Compilation(syntaxTree);
                var compilationResult = compilation.Evaluate(variables);

                var diagnostics = compilationResult.Diagnostics;

                if (_showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    syntaxTree.Root.WriteTo(Console.Out);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    Console.WriteLine(compilationResult.Value);
                    continue;
                }


                var text = syntaxTree.Text;
                foreach (var diagnostic in diagnostics)
                {
                    var lineIndex = text.GetLineIndex(diagnostic.Span.Start);
                    var diagnosticLine = text.Lines[lineIndex];
                    var character = diagnostic.Span.Start - diagnosticLine.Start + 1;
                    var lineNumber = lineIndex + 1;


                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write($"({lineNumber}, {character}): ");
                    Console.WriteLine(diagnostic.Message);
                    Console.ResetColor();

                    var prefix = line.Substring(0, diagnostic.Span.Start);
                    var error = line.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
                    var suffix = line.Substring(diagnostic.Span.End);
                    
                    Console.Write(prefix);

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(error);
                    Console.ResetColor();

                    Console.WriteLine(suffix);
                }
                Console.WriteLine();
            }
        }

        private static bool CheckCommands(string line)
        {
            if (line == "#tree")
            {
                _showTree = !_showTree;
                Console.WriteLine(_showTree ? "Showing parse trees" : "Not showing parse trees");
                return true;
            }

            if (line == "#cls")
            {
                Console.Clear();
                return true;
            }

            return false;
        }
    }
}