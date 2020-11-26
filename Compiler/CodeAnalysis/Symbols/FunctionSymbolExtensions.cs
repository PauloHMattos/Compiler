using System.Collections.Immutable;
using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public static class FunctionSymbolExtensions
    {
        public static bool SameSignature(this FunctionSymbol function, FunctionSymbol otherFunction)
        {
            if (function.Parameters.Length != otherFunction.Parameters.Length)
            {
                return false;
            }

            for (var i = 0; i < function.Parameters.Length; i++)
            {
                if (function.Parameters[i].Type != otherFunction.Parameters[i].Type)
                {
                    return false;
                }
            }
            
            return true;
        }

        internal static FunctionSymbol? MatchArgumentsAndParameters(this FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
        {
            if(Match(function, arguments))
            {
                return function;
            }

            foreach(var overload in function.Overloads)
            {
                if (Match(overload, arguments))
                {
                    return overload;
                }
            }

            return null;

            static bool Match(FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
            {
                if (function.Parameters.Length != arguments.Length)
                {
                    return false;
                }

                for (var i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    var expected = function.Parameters[i];
                    var conversion = Conversion.Classify(argument.Type, expected.Type);

                    if (!conversion.Exists || conversion.IsExplicit)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}