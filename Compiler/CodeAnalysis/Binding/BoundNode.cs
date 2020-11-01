using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Compiler.CodeAnalysis.Binding
{
    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }

        public IEnumerable<BoundNode> GetChildren()
        {
            var properties = GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType))
                {
                    var value = (BoundNode)property.GetValue(this);
                    if (value == null)
                    {
                        continue;
                    }
                    yield return value;
                }
                else if (typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                {
                    var values = (IEnumerable<BoundNode>)property.GetValue(this);
                    foreach (var value in values)
                    {
                        if (value == null)
                        {
                            continue;
                        }
                        yield return value;
                    }
                }
            }
        }

        private IEnumerable<(string Name, object Value)> GetProperties()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.Name == nameof(Kind) ||
                    property.Name == nameof(BoundBinaryExpression.Operator))
                    continue;

                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType) ||
                    typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                    continue;

                var value = property.GetValue(this);
                if (value != null)
                    yield return (property.Name, value);
            }
        }

        public void WriteTo(TextWriter writer)
        {
            PrintTree(writer, this);
        }

        private static void PrintTree(TextWriter writer, BoundNode node, string indent = "", bool isLast = true)
        {
            var isToConsole = writer == Console.Out;
            var marker = isLast ? "└──" : "├──";

            if (isToConsole)
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write(indent);
            writer.Write(marker);

            if (isToConsole)
                Console.ForegroundColor = GetColor(node);

            var text = GetText(node);
            writer.Write(text);

            var isFirstProperty = true;

            foreach (var p in node.GetProperties())
            {
                if (isFirstProperty)
                    isFirstProperty = false;
                else
                {
                    if (isToConsole)
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                    writer.Write(",");
                }

                writer.Write(" ");

                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.Yellow;

                writer.Write(p.Name);

                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                writer.Write(" = ");

                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;

                writer.Write(p.Value);
            }

            if (isToConsole)
                Console.ResetColor();

            writer.WriteLine();

            indent += isLast ? "   " : "│  ";

            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
                PrintTree(writer, child, indent, child == lastChild);
        }

        private static string GetText(BoundNode node)
        {
            switch (node)
            {
                case BoundBinaryExpression b:
                    return b.Operator.Kind + "Expression";
                case BoundUnaryExpression u:
                    return u.Operator.Kind + "Expression";
                default:
                    return node.Kind.ToString();
            }
        }

        private static ConsoleColor GetColor(BoundNode node)
        {
            switch (node)
            {
                case BoundExpression _:
                    return ConsoleColor.Blue;
                case BoundStatement _:
                    return ConsoleColor.Cyan;
                default:
                    return ConsoleColor.Yellow;
            }
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }
    }
}