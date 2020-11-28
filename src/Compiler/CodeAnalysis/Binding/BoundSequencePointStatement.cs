using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundSequencePointStatement : BoundStatement
    {
        public BoundStatement Statement { get; }
        public TextLocation Location { get; }
        public override BoundNodeKind Kind => BoundNodeKind.SequencePointStatement;

        public BoundSequencePointStatement(SyntaxNode syntax,
                                           BoundStatement statement,
                                           TextLocation location) 
            : base(syntax)
        {
            Statement = statement;
            Location = location;
        }
    }
}