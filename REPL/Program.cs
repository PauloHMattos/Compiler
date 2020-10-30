using System;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Evaluation;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.REPL
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var showTree = false;
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                if (line == "#tree")
                {
                    showTree = !showTree;
                    Console.WriteLine(showTree ? "Showing parse trees" : "Not showing parse trees");
                    continue;
                }
                if (line == "#cls")
                {
                    Console.Clear();
                    continue;
                }

                var syntaxTree = SyntaxTree.Parse(line);
                var boundExpression = Binder.Bind(syntaxTree.Root, out var binderDiagnostics);
                var diagnostics = syntaxTree.Diagnostics.Concat(binderDiagnostics).ToArray();

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrintSyntaxTree(syntaxTree.Root);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    var evaluator = new Evaluator(boundExpression);
                    var result = evaluator.Evaluate();
                    Console.WriteLine(result);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    foreach (var diagnostic in diagnostics)
                    {
                        Console.WriteLine(diagnostic);
                    }
                    Console.ResetColor();
                }
            }
        }

        private static void PrintSyntaxTree(SyntaxNode node, string indent = "", bool isLast = true)
        {
            // └──
            // │
            // ├──
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