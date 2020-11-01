using System.Collections.Immutable;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Lowering
{
    internal class Lowerer : BoundTreeRewriter
    {
        private Lowerer()
        {
        }

        public static BoundStatement Lower(BoundStatement statement)
        {
            var lowerer = new Lowerer();
            return lowerer.RewriteStatement(statement);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            // for <variable> = <lower> to <upper> step <step>
            // {
            //      <body>
            // }
            //
            //
            // var <variable> = <lower>
            // while (<var> <= <upper>)
            // {
            //      <body>
            //      <var> = <var> + <step>
            // }

            var variableDeclaration = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);
            var variableExpression = new BoundVariableExpression(node.Variable);
            
            var condition = new BoundBinaryExpression(variableExpression,
                BoundBinaryOperator.Bind(SyntaxKind.LessOrEqualsToken, TypeSymbol.Int, TypeSymbol.Int), 
                node.UpperBound);

            var stepIncrement = new BoundExpressionStatement( 
                new BoundAssignmentExpression(node.Variable, 
                    new BoundBinaryExpression(variableExpression,
                        BoundBinaryOperator.Bind(SyntaxKind.PlusToken, TypeSymbol.Int, TypeSymbol.Int), 
                        node.Step))
                );

            var whileBody = new BoundBlockStatement(ImmutableArray.Create(node.Body, stepIncrement));
            var whileStatement = new BoundWhileStatement(condition, whileBody);

            var result =
                new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(variableDeclaration, whileStatement));
            return RewriteStatement(result);
        }
    }
}
