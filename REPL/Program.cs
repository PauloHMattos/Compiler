using System;
using System.Linq;
using Compiler.CodeAnalysis;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.REPL
{
    internal static class Program
    {
        private static bool _showTree;

        private static void Main(string[] args)
        {
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
                var compilationResult = compilation.Evaluate();

                var diagnostics = compilationResult.Diagnostics;

                if (_showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrintSyntaxTree(syntaxTree.Root);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    Console.WriteLine(compilationResult.Value);
                    continue;
                }

                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.DarkRed;
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

        private static void PrintSyntaxTree(SyntaxNode node, string indent = "", bool isLast = true)
        {
            var marker = isLast ? "└──" : "├──";

            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);
            if (node is SyntaxToken t && t.Value != null)
            {
                Console.Write($" {t.Value}");
            }

            Console.WriteLine();

            indent += isLast ? "   " : "│  ";


            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
            {
                PrintSyntaxTree(child, indent, child == lastChild);
            }
        }
    }
}