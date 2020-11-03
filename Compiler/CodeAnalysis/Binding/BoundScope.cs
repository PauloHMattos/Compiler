using System.Collections.Generic;
using System.Collections.Immutable;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        public BoundScope Parent { get; }
        private readonly Dictionary<string, VariableSymbol> _variables;
        private readonly Dictionary<string, FunctionSymbol> _functions;

        public BoundScope(BoundScope parent)
        {
            Parent = parent;
            _variables = new Dictionary<string, VariableSymbol>();
            _functions = new Dictionary<string, FunctionSymbol>();
        }

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            if (_variables.ContainsKey(variable.Name))
            {
                return false;
            }
            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookupVariable(string name, out VariableSymbol variable)
        {
            if (_variables.TryGetValue(name, out variable))
            {
                return true;
            }

            if (Parent == null)
            {
                return false;
            }
            return Parent.TryLookupVariable(name, out variable);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => _variables.Values.ToImmutableArray();

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            if (_functions.ContainsKey(function.Name))
            {
                return false;
            }
            _functions.Add(function.Name, function);
            return true;
        }

        public bool TryLookupFunction(string name, out FunctionSymbol function)
        {
            if (_functions.TryGetValue(name, out function))
            {
                return true;
            }

            if (Parent == null)
            {
                return false;
            }
            return Parent.TryLookupFunction(name, out function);
        }

        public ImmutableArray<FunctionSymbol> GetDeclaredFunction() => _functions.Values.ToImmutableArray();
    }
}