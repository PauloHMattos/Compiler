using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler.CodeAnalysis.Symbols
{
    public class TypeSymbol : Symbol
    {
        // Special types
        public static readonly TypeSymbol Error = new TypeSymbol("?", null, null);
        // Base types
        public static readonly TypeSymbol Enum = new TypeSymbol("enum", null, typeof(Enum));
        public static readonly TypeSymbol Struct = new TypeSymbol("struct", null, typeof(ValueType));
        // Built-in types
        public static readonly TypeSymbol Any = new TypeSymbol("any", null, typeof(object));
        public static readonly TypeSymbol Void = new TypeSymbol("void", null, typeof(void));
        public static readonly TypeSymbol Bool = new TypeSymbol("bool", default(bool), typeof(bool));
        public static readonly TypeSymbol Int = new TypeSymbol("int", default(int), typeof(int));
        public static readonly TypeSymbol String = new TypeSymbol("string", string.Empty, typeof(string));

        public Type? NetType { get; }
        public object? DefaultValue { get; }
        public override SymbolKind Kind => SymbolKind.Type;
        public virtual ImmutableArray<MemberSymbol> Members { get; }

        protected TypeSymbol(string name, object? defaultValue, Type? netType, ImmutableArray<MemberSymbol> members) : base(name)
        {
            NetType = netType;
            Members = members;
            DefaultValue = defaultValue;
        }
        
        protected TypeSymbol(string name, object? defaultValue, Type? netType)
            : this(name, defaultValue, netType, ImmutableArray<MemberSymbol>.Empty)
        {
            NetType = netType;
            DefaultValue = defaultValue;
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

        public static TypeSymbol? LookupType(string name)
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

        internal bool IsEnum()
        {
            if (NetType == null)
            {
                return false;
            }
            return NetType == typeof(Enum) || NetType.IsEnum;
        }

        public static IEnumerable<TypeSymbol> GetBuiltInTypes()
        {
            yield return Any;
            yield return Int;
            yield return Bool;
            yield return String;
            yield return Void;
        }
        
        public static IEnumerable<TypeSymbol> GetBaseTypes()
        {
            yield return Enum;
            yield return Struct;
        }
    }
}