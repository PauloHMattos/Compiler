﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }

        public virtual TextSpan Span
        {
            get
            {
                var children = GetChildren();
                var first = children.First().Span;
                var last = children.Last().Span;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public IEnumerable<SyntaxNode> GetChildren()
        {
            var properties = GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))
                {
                    yield return (SyntaxNode)property.GetValue(this);
                }
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                {
                    var values = (IEnumerable<SyntaxNode>)property.GetValue(this);
                    foreach (var value in values)
                    {
                        yield return value;
                    }
                }
            }
        }

        public void WriteTo(TextWriter writer)
        {
            PrintSyntaxTree(writer, this);
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }

        private static void PrintSyntaxTree(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
        {
            var marker = isLast ? "└──" : "├──";

            writer.Write(indent);
            writer.Write(marker);
            writer.Write(node.Kind);
            if (node is SyntaxToken t && t.Value != null)
            {
                writer.Write($" {t.Value}");
            }

            writer.WriteLine();

            indent += isLast ? "   " : "│  ";


            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
            {
                PrintSyntaxTree(writer, child, indent, child == lastChild);
            }
        }
    }
}