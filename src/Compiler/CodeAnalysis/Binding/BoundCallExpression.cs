using System.Collections.Immutable;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundCallExpression : BoundMemberExpression
    {
        public ImmutableArray<BoundExpression> Arguments { get; }
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;

        public BoundCallExpression(SyntaxNode syntax,
                                   FunctionSymbol function,
                                   ImmutableArray<BoundExpression> arguments)
            : base(syntax, function)
        {
            Arguments = arguments;
        }
    }
}