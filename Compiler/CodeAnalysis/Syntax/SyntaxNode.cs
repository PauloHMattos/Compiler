using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }
        public SyntaxTree SyntaxTree { get; }
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

        public TextLocation Location => new TextLocation(SyntaxTree.Text, Span);

        protected SyntaxNode(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        public SyntaxToken GetLastToken()
        {
            if (this is SyntaxToken token)
                return token;

            // A syntax node should always contain at least 1 token.
            return GetChildren().Last().GetLastToken();
        }

        public IEnumerable<SyntaxNode> GetChildren()
        {
            var properties = GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))
                {
                    var value = (SyntaxNode) property.GetValue(this);
                    if (value == null)
                    {
                        continue;
                    }
                    yield return value;
                }
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                {
                    var values = (IEnumerable<SyntaxNode>)property.GetValue(this);
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

        public void WriteTo(TextWriter writer)
        {
            PrintTree(writer, this);
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer);
            return writer.ToString();
        }

        private static void PrintTree(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
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
                PrintTree(writer, child, indent, child == lastChild);
            }
        }
    }
}