using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundSelfExpression : BoundExpression
    {
        public TypeSymbol Instance { get; }
        public override TypeSymbol Type => Instance;
        public override BoundNodeKind Kind => BoundNodeKind.SelfExpression;

        public BoundSelfExpression(SyntaxNode syntax, TypeSymbol instance)
            : base(syntax)
        {
            Instance = instance;
        }
    }
}