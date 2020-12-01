using System.Collections.Immutable;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal class BoundCallExpression : BoundExpression
    {
        public BoundExpression? Instance { get; }
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.ReturnType;

        public BoundCallExpression(SyntaxNode syntax,
                                   FunctionSymbol function,
                                   ImmutableArray<BoundExpression> arguments)
            : base(syntax)
        {
            Function = function;
            Arguments = arguments;
        }

        public BoundCallExpression(SyntaxNode syntax,
                                   BoundVariableExpression instance,
                                   FunctionSymbol function,
                                   ImmutableArray<BoundExpression> arguments)
            : this (syntax, function, arguments)
        {
            Instance = instance;
        }

        public BoundCallExpression(SyntaxNode syntax,
                                   BoundMemberAccessExpression instance,
                                   FunctionSymbol function,
                                   ImmutableArray<BoundExpression> arguments)
            : this (syntax, function, arguments)
        {
            Instance = instance;
        }

        public BoundCallExpression(SyntaxNode syntax,
                                   BoundSelfExpression instance,
                                   FunctionSymbol function,
                                   ImmutableArray<BoundExpression> arguments)
            :this (syntax, function, arguments)
        {
            Instance = instance;
        }
    }
}