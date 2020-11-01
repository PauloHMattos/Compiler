using System;

namespace Compiler.CodeAnalysis.Binding
{
    internal static class TypeSymbol
    {
        public static Type Bool => typeof(bool);
        public static Type Int => typeof(int);

        public static object DefaultValue(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}