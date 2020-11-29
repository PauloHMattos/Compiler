using Compiler.CodeAnalysis.Binding;

namespace Compiler.CodeAnalysis.Symbols
{
    public enum MemberKind
    {
        Field,
        Property,
        Method
    }

    public abstract class MemberSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.Member;
        public abstract MemberKind MemberKind { get; }

        private protected MemberSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constant) : base(name, isReadOnly, type, constant)
        {
        }
    }
    
    public class FieldSymbol : MemberSymbol
    {
        public override MemberKind MemberKind => MemberKind.Field;

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