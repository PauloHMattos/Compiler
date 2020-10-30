using System;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class TypeSymbol
    {
        public static Type Bool => typeof(bool);
        public static Type Int => typeof(int);

        public static object DefaultValue(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}