using System;

namespace Compiler.CodeAnalysis.Symbols
{
    public class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("?");
        public static readonly TypeSymbol Void = new TypeSymbol("void");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol Int = new TypeSymbol("int");
        public static readonly TypeSymbol String = new TypeSymbol("string");

        public override SymbolKind Kind => SymbolKind.Type;


        private TypeSymbol(string name) : base(name)
        {

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
    }
}