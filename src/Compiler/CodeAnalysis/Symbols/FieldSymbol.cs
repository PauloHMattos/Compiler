using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class FieldSymbol : MemberSymbol
    {
        public bool IsReadOnly { get; }
        public override MemberKind MemberKind => MemberKind.Field;

        private protected FieldSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constant)
            : base(name, type, isReadOnly ? constant : null)
        {
            IsReadOnly = isReadOnly;
        }


        private protected FieldSymbol(VariableSymbol variable) 
            : this(variable.Name, variable.IsReadOnly, variable.Type, variable.Constant)
        {
        }

        internal FieldSymbol(BoundVariableDeclarationStatement declaration)
            : this(declaration.Variable)
        {
        }
    }
}