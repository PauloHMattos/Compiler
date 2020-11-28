using System.Collections.Immutable;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundBlockStatement : BoundStatement
    {
        public ImmutableArray<BoundStatement> Statements { get; }
        public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;

        public BoundBlockStatement(SyntaxNode syntax, ImmutableArray<BoundStatement> statements)
            : base(syntax)
        {
            Statements = statements;
        }
    }
}