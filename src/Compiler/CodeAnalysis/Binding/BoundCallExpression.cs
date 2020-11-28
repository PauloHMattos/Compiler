using System.Collections.Immutable;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundCallExpression : BoundExpression
    {
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.Type;

        public BoundCallExpression(SyntaxNode syntax, FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
            : base(syntax)
        {
            Function = function;
            Arguments = arguments;
        }
    }
}