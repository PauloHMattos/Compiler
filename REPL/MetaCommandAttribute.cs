using System;

namespace Compiler.REPL
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    internal sealed class MetaCommandAttribute : Attribute
    {
        public MetaCommandAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }
    }
}