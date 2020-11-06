using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Lowering
{
    internal class Lowerer : BoundTreeRewriter
    {
        private int _labelCount;

        private Lowerer()
        {
        }

        private BoundLabel GenerateNewLabel()
        {
            var name = $"LABEL_{++_labelCount}";
            return new BoundLabel(name);
        }

        public static BoundBlockStatement Lower(FunctionSymbol function, BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return Flatten(function, result);
        }

        private static BoundBlockStatement Flatten(FunctionSymbol function, BoundStatement statement)
        {
            var builder = ImmutableArray.CreateBuilder<BoundStatement>();
            var stack = new Stack<BoundStatement>();
            stack.Push(statement);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is BoundBlockStatement block)
                {
                    foreach (var s in block.Statements.Reverse())
                    {
                        stack.Push(s);
                    }
                }
                else
                {
                    builder.Add(current);
                }
            }

            if (function.Type == TypeSymbol.Void)
            {
                if (builder.Count == 0 || CanFallThrough(builder.Last()))
                {
                    builder.Add(new BoundReturnStatement(null));
                }
            }

            return new BoundBlockStatement(builder.ToImmutable());
        }
        
        private static bool CanFallThrough(BoundStatement boundStatement)
        {
            // TODO: We don't rewrite conditional gotos where the condition is
            //       always true. We shouldn't handle this here, because we
            //       should really rewrite those to unconditional gotos in the
            //       first place.
            return boundStatement.Kind != BoundNodeKind.ReturnStatement &&
                   boundStatement.Kind != BoundNodeKind.GotoStatement;
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundBlockStatement result;
            if (node.ElseStatement == null)
            {
                // if <condition> 
                //      <then>
                //
                // ---------------->
                //
                // gotoIfFalse <condition> end
                // <then>
                // end:

                var endLabel = GenerateNewLabel();
                var gotoFalse = new BoundConditionalGotoStatement(endLabel, node.Condition, false);
                var endLabelStatement = new BoundLabelStatement(endLabel);
                result = new BoundBlockStatement(ImmutableArray.Create(
                        gotoFalse,
                        node.ThenStatement, 
                        endLabelStatement
                    ));

            }
            else
            {
                // if <condition> 
                //      <then>
                // else
                //      <else>
                //
                // ---------------->
                //
                // gotoIfFalse <condition> else
                // <then>
                // goto end
                // else:
                // <else>
                // end:

                var elseLabel = GenerateNewLabel();
                var endLabel = GenerateNewLabel();
                var gotoFalse = new BoundConditionalGotoStatement(elseLabel, node.Condition, false);
                var gotoEnd = new BoundGotoStatement(endLabel);

                var elseLabelStatement = new BoundLabelStatement(elseLabel);
                var endLabelStatement = new BoundLabelStatement(endLabel);

                result = new BoundBlockStatement(ImmutableArray.Create(
                    gotoFalse,
                    node.ThenStatement,
                    gotoEnd,
                    elseLabelStatement,
                    node.ElseStatement,
                    endLabelStatement
                ));
            }
            return RewriteStatement(result);
        }


        protected override BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            // do
            //      <body>
            // while <condition>
            //
            // ---------------->
            //
            // body:
            //      <body>
            // continue:
            //      gotoTrue <condition> body
            // break:

            var bodyLabel = GenerateNewLabel();


            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var bodyLabelStatement = new BoundLabelStatement(bodyLabel);

            var gotoTrue = new BoundConditionalGotoStatement(bodyLabel, node.Condition, true);

            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);

            var result = new BoundBlockStatement(ImmutableArray.Create(
                bodyLabelStatement,
                node.Body,
                continueLabelStatement,
                gotoTrue,
                breakLabelStatement
            ));
            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            // while <condition>
            //      <body>
            //
            // ---------------->
            //
            // goto continue
            // body:
            //      <body>
            // continue:
            //      gotoTrue <condition> continue
            // break:

            var bodyLabel = GenerateNewLabel();

            var gotoContinue = new BoundGotoStatement(node.ContinueLabel);
            var bodyLabelStatement = new BoundLabelStatement(bodyLabel);
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);

            var gotoTrue = new BoundConditionalGotoStatement(bodyLabel, node.Condition, true);

            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);

            var result = new BoundBlockStatement(ImmutableArray.Create(
                gotoContinue,
                bodyLabelStatement,
                node.Body,
                continueLabelStatement,
                gotoTrue,
                breakLabelStatement
            ));
            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            // for <variable> = <lower> to <upper> step <step>
            //      <body>
            //
            // --->
            //
            // var <variable> = <lower>
            // while (<var> <= <upper>)
            //      <body>
            //      continue:
            //      <var> = <var> + <step>

            var variableDeclaration = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);
            var variableExpression = new BoundVariableExpression(node.Variable);
            var upperBoundSymbol = new LocalVariableSymbol("upperBound", true, TypeSymbol.Int);
            var upperBoundDeclaration = new BoundVariableDeclarationStatement(upperBoundSymbol, node.UpperBound);
            var condition = new BoundBinaryExpression(
                variableExpression,
                BoundBinaryOperator.Bind(SyntaxKind.LessOrEqualsToken, TypeSymbol.Int, TypeSymbol.Int),
                new BoundVariableExpression(upperBoundSymbol)
            );
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var stepIncrement = new BoundExpressionStatement( 
                new BoundAssignmentExpression(node.Variable, 
                    new BoundBinaryExpression(variableExpression,
                        BoundBinaryOperator.Bind(SyntaxKind.PlusToken, TypeSymbol.Int, TypeSymbol.Int), 
                        node.Step))
                );

            var whileBody = new BoundBlockStatement(ImmutableArray.Create(
                node.Body,
                continueLabelStatement,
                stepIncrement));
            var whileStatement = new BoundWhileStatement(condition, whileBody, node.BreakLabel, GenerateNewLabel());

            var result =
                new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(variableDeclaration, upperBoundDeclaration, whileStatement));
            return RewriteStatement(result);
        }
    }
}
