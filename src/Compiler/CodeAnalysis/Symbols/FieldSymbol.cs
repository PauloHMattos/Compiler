using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Symbols
{
    public sealed class FieldSymbol : MemberSymbol
    {
        internal BoundExpression Initializer { get; }
        public override MemberKind MemberKind => MemberKind.Field;


        private FieldSymbol(SyntaxNode? syntax,
                            string name,
                            bool isReadOnly,
                            bool isStatic,
                            TypeSymbol type,
                            BoundExpression initializer)
            : base(syntax, name, isReadOnly, isStatic, type, initializer.ConstantValue)
        {
            Initializer = initializer;
        }


        internal FieldSymbol(VariableSymbol variable, BoundExpression initializer) 
            : this(variable.Syntax,
                   variable.Name,
                   variable.IsReadOnly,
                   variable.IsStatic,
                   variable.Type,
                   initializer)
        {
        }

        internal FieldSymbol(BoundVariableDeclarationStatement declaration)
            : this(declaration.Variable, declaration.Initializer)
        {
        }
    }
}