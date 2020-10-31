using System.Collections.Generic;
using System.Reflection;

namespace Compiler.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }

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
    }
}