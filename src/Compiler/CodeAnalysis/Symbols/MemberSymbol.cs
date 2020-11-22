using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public abstract class MemberSymbol : VariableSymbol
    {
        private protected MemberSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constant) : base(name, isReadOnly, type, constant)
        {
        }
    }
    
    public class FieldSymbol : MemberSymbol
    {
        public override SymbolKind Kind => SymbolKind.Field;

        private protected FieldSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constant) : base(name, isReadOnly, type, constant)
        {
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