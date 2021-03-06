﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Compiler.CodeAnalysis.Syntax.Attributes;
using Compiler.CodeAnalysis.Text;
using Compiler.IO;

namespace Compiler.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }
        public SyntaxTree SyntaxTree { get; }
        public SyntaxNode? Parent => SyntaxTree.GetParent(this);
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

        public virtual TextSpan FullSpan
        {
            get
            {
                var children = GetChildren();
                var first = children.First().FullSpan;
                var last = children.Last().FullSpan;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public TextLocation Location => new TextLocation(SyntaxTree.Text, Span);

        private protected SyntaxNode(SyntaxTree syntaxTree)
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

        public IEnumerable<SyntaxNode> AncestorsAndSelf()
        {
            var node = this;
            while (node != null)
            {
                yield return node;
                node = node.Parent;
            }
        }

        public IEnumerable<SyntaxNode> Ancestors()
        {
            return AncestorsAndSelf().Skip(1);
        }

        public virtual IEnumerable<SyntaxNode> GetChildren()
        {
            var properties = GetType().
                    GetProperties(BindingFlags.Public | BindingFlags.Instance).
                    Where(p => p.Name != nameof(Parent) &&
                               p.GetCustomAttribute<DiscardFromChildrenAttribute>() == null);	

            foreach (var property in properties)	
            {	
                if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))	
                {	
                    var child = (SyntaxNode?)property.GetValue(this);	
                    if (child != null)	
                        yield return child;	
                }	
                else if (typeof(SeparatedSyntaxList).IsAssignableFrom(property.PropertyType))	
                {	
                    var separatedSyntaxList = (SeparatedSyntaxList)property.GetValue(this)!;	
                    foreach (var child in separatedSyntaxList.GetWithSeparators())	
                        yield return child;	
                }	
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))	
                {	
                    var children = (IEnumerable<SyntaxNode>)property.GetValue(this)!;	
                    foreach (var child in children)	
                    {	
                        if (child != null)	
                            yield return child;	
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
            var token = node as SyntaxToken;
            if (token != null)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    writer.SetForeground(ConsoleColor.DarkGray);
                    writer.Write(indent);
                    writer.Write("├──");

                    writer.SetForeground(ConsoleColor.DarkGreen);
                    writer.WriteLine($"L: {trivia.Kind}");
                    writer.ResetColor();
                }
            }


            var hasTrailingTrivia = token != null && token.TrailingTrivia.Any();
            var marker = !hasTrailingTrivia && isLast ? "└──" : "├──";

            writer.SetForeground(ConsoleColor.DarkGray);
            writer.Write(indent);
            writer.Write(marker);

            writer.SetForeground(token != null ? ConsoleColor.Blue : ConsoleColor.Cyan);
            writer.Write(node.Kind);
            if (token != null && token.Kind == SyntaxKind.IdentifierToken)
            {
                writer.Write(" (");
                writer.Write(token.Text);
                writer.Write(")");
            }
            
            if (token != null && token.Value != null)
            {
                writer.Write($" {token.Value}");
            }

            writer.WriteLine();
            writer.ResetColor();

            if (token != null)
            {
                foreach (var trivia in token.TrailingTrivia)
                {
                    var isLastTrailingTrivia = trivia == token.TrailingTrivia.Last();
                    var triviaMarker = isLast && isLastTrailingTrivia ? "└──" : "├──";

                    writer.SetForeground(ConsoleColor.DarkGray);
                    writer.Write(indent);
                    writer.Write(triviaMarker);

                    writer.SetForeground(ConsoleColor.DarkGreen);
                    writer.WriteLine($"T: {trivia.Kind}");
                }
            }


            indent += isLast ? "   " : "│  ";


            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
            {
                PrintTree(writer, child, indent, child == lastChild);
            }
        }
    }
}