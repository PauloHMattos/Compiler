using System;

namespace Compiler.CodeAnalysis.Syntax.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class DiscardFromChildrenAttribute : Attribute
    {
    }
}