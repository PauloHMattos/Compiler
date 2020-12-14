using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class FieldSymbol : MemberSymbol
    {
        public override MemberKind MemberKind => MemberKind.Field;

        private FieldSymbol(string name, bool isReadOnly, bool isStatic, TypeSymbol type, BoundConstant? constant)
            : base(name, isReadOnly, isStatic, type, constant)
        {
        }


        internal FieldSymbol(VariableSymbol variable) 
            : this(variable.Name, variable.IsReadOnly, variable.IsStatic, variable.Type, variable.Constant)
        {
        }

        internal FieldSymbol(BoundVariableDeclarationStatement declaration)
            : this(declaration.Variable)
        {
        }
    }
}