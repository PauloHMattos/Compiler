using System;
using System.Collections.Generic;

namespace Compiler.CodeAnalysis.Symbols
{
    public class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("?", null);
        public static readonly TypeSymbol Any = new TypeSymbol("any", typeof(object));
        public static readonly TypeSymbol Void = new TypeSymbol("void", typeof(void));
        public static readonly TypeSymbol Bool = new TypeSymbol("bool", typeof(bool));
        public static readonly TypeSymbol Int = new TypeSymbol("int", typeof(int));
        public static readonly TypeSymbol String = new TypeSymbol("string", typeof(string));

        public Type NetType { get; }
        public override SymbolKind Kind => SymbolKind.Type;


        private TypeSymbol(string name, Type netType) : base(name)
        {
            NetType = netType;
        }

        public static TypeSymbol GetSymbolFrom(object value)
        {
            switch (value)
            {
                case bool _:
                    return Bool;
                case int _:
                    return Int;
                case string _:
                    return String;
                default:
                    throw new InvalidOperationException($"Unexpected literal '{value}' of type '{value.GetType()}'");
            }
        }

        public static TypeSymbol LookupType(string name)
        {
            switch (name)
            {
                case "any":
                    return TypeSymbol.Any;
                case "bool":
                    return TypeSymbol.Bool;
                case "int":
                    return TypeSymbol.Int;
                case "string":
                    return TypeSymbol.String;
                default:
                    return null;
            }
        }

        public static IEnumerable<TypeSymbol> GetBuiltInTypes()
        {
            yield return Any;
            yield return Int;
            yield return Bool;
            yield return String;
            yield return Void;
        }
    }
}