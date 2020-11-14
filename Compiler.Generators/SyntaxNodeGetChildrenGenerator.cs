﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Text;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace Compiler.Generators
{
    [Generator]
    public class SyntaxNodeGetChildrenGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            SourceText sourceText;
            var compilation = (CSharpCompilation)context.Compilation;

            var immutableArrayType = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
            var separatedSyntaxListType = compilation.GetTypeByMetadataName("Compiler.CodeAnalysis.Syntax.SeparatedSyntaxList`1");
            var syntaxNodeType = compilation.GetTypeByMetadataName("Compiler.CodeAnalysis.Syntax.SyntaxNode");

            var types = GetAllTypes(compilation.Assembly);
            var syntaxNodeTypes = types.Where(t => !t.IsAbstract && IsPartial(t) && IsDerivedFrom(t, syntaxNodeType));

            using (var stringWriter = new StringWriter())
            using (var indentedTextWriter = new IndentedTextWriter(stringWriter, "    "))
            {
                indentedTextWriter.WriteLine("using System;");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine("using System.Collections.Immutable;");
                indentedTextWriter.WriteLine();
                indentedTextWriter.WriteLine("namespace Compiler.CodeAnalysis.Syntax");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;

                foreach (var type in syntaxNodeTypes)
                {
                    Console.WriteLine(type.Name);
                    indentedTextWriter.WriteLine($"partial class {type.Name}");
                    indentedTextWriter.WriteLine("{");
                    indentedTextWriter.Indent++;

                    indentedTextWriter.WriteLine("public override IEnumerable<SyntaxNode> GetChildren()");
                    indentedTextWriter.WriteLine("{");
                    indentedTextWriter.Indent++;

                    foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (property.Type is INamedTypeSymbol propertyType)
                        {
                            if (IsDerivedFrom(propertyType, syntaxNodeType))
                            {
                                indentedTextWriter.WriteLine($"yield return {property.Name};");
                            }
                            else if (propertyType.TypeArguments.Length == 1 &&
                                     IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType) &&
                                     SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, immutableArrayType))
                            {
                                indentedTextWriter.WriteLine($"foreach (var child in {property.Name})");
                                indentedTextWriter.WriteLine("{");
                                indentedTextWriter.Indent++;
                                indentedTextWriter.WriteLine("yield return child;");
                                indentedTextWriter.Indent--;
                                indentedTextWriter.WriteLine("}");
                            }
                            else if (SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, separatedSyntaxListType) &&
                                     IsDerivedFrom(propertyType.TypeArguments[0], syntaxNodeType))
                            {
                                indentedTextWriter.WriteLine($"foreach (var child in {property.Name}.GetWithSeparators())");
                                indentedTextWriter.WriteLine("{");
                                indentedTextWriter.Indent++;
                                indentedTextWriter.WriteLine("yield return child;");
                                indentedTextWriter.Indent--;
                                indentedTextWriter.WriteLine("}");
                            }
                        }
                    }

                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("}");

                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("}");
                }

                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");

                indentedTextWriter.Flush();
                stringWriter.Flush();

                sourceText = SourceText.From(stringWriter.ToString(), Encoding.UTF8);
            }

            var hintName = "SyntaxNode_GetChildren.g.cs";
            context.AddSource(hintName, sourceText);
        }

        private IReadOnlyList<INamedTypeSymbol> GetAllTypes(IAssemblySymbol symbol)
        {
            var result = new List<INamedTypeSymbol>();
            GetAllTypes(result, symbol.GlobalNamespace);
            result.Sort((x, y) => x.MetadataName.CompareTo(y.MetadataName));
            return result;
        }

        private void GetAllTypes(List<INamedTypeSymbol> result, INamespaceOrTypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol type)
            {
                result.Add(type);
            }

            foreach (var child in symbol.GetMembers())
            {
                if (child is INamespaceOrTypeSymbol nsChild)
                {
                    GetAllTypes(result, nsChild);
                }
            }
        }

        private bool IsDerivedFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            while (type != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type, baseType))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private bool IsPartial(INamedTypeSymbol type)
        {
            foreach (var declaration in type.DeclaringSyntaxReferences)
            {
                var syntax = declaration.GetSyntax();
                if (syntax is TypeDeclarationSyntax typeDeclaration)
                {
                    foreach (var modifer in typeDeclaration.Modifiers)
                    {
                        if (modifer.ValueText == "partial")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
