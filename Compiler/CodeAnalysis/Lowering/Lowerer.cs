using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Symbols;
using static Compiler.CodeAnalysis.Binding.BoundNodeFactory;

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
            return RemoveDeadCode(Flatten(function, result));
        }

        private static BoundBlockStatement RemoveDeadCode(BoundBlockStatement node)
        {
            var controlFlow = ControlFlowGraph.Create(node);
            var reachableStatements = new HashSet<BoundStatement>(controlFlow.Blocks.SelectMany(b => b.Statements));

            var builder = node.Statements.ToBuilder();
            for (int i = builder.Count - 1; i >= 0; i--)
            {
                if (!reachableStatements.Contains(builder[i]))
                {
                    builder.RemoveAt(i);
                }
            }

            return new BoundBlockStatement(builder.ToImmutable());
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

            if (function.Type == TypeSymbol.Void &&
                (builder.Count == 0 || CanFallThrough(builder.Last())))
            {
                builder.Add(new BoundReturnStatement(null));
            }

            return new BoundBlockStatement(builder.ToImmutable());
        }

        private static bool CanFallThrough(BoundStatement boundStatement)
        {
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
                var endLabel = Label(GenerateNewLabel());
                result = Block(GotoFalse(endLabel, node.Condition), node.ThenStatement, endLabel);
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

                var elseLabel = Label(GenerateNewLabel());
                var endLabel = Label(GenerateNewLabel());
                result = Block(GotoFalse(elseLabel, node.Condition),
                                   node.ThenStatement,
                                   Goto(endLabel),
                                   elseLabel,
                                   node.ElseStatement,
                                   endLabel);
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

            var bodyLabel = Label(GenerateNewLabel());
            var result = Block(bodyLabel,
                               node.Body,
                               Label(node.ContinueLabel),
                               GotoTrue(bodyLabel, node.Condition),
                               Label(node.BreakLabel));

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

            var bodyLabel = Label(GenerateNewLabel());

            var result = Block(Goto(node.ContinueLabel),
                               bodyLabel,
                               node.Body,
                               Label(node.ContinueLabel),
                               GotoTrue(bodyLabel, node.Condition),
                               Label(node.BreakLabel));

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

            var lowerBound = VariableDeclaration(node.Variable, node.LowerBound);
            var upperBound = ConstantDeclaration("upperBound", node.UpperBound);

            var result = Block(lowerBound, 
                               upperBound, 
                               While(LessOrEqual(Variable(lowerBound),Variable(upperBound)),
                                    Block(node.Body,
                                        Label(node.ContinueLabel),
                                        Increment(Variable(lowerBound), node.Step)),
                                    node.BreakLabel,
                                    continueLabel: GenerateNewLabel())
            );

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition.ConstantValue != null)
            {
                var condition = (bool)node.Condition.ConstantValue.Value;
                condition = node.JumpIfTrue ? condition : !condition;
                if (condition)
                {
                    return RewriteStatement(Goto(node.Label));
                }
                else
                {
                    return RewriteStatement(Nop());
                }
            }

            return base.RewriteConditionalGotoStatement(node);
        }

        protected override BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            // a <op>= b
            //
            // --->
            //
            // a = (a <op> b)

            var newNode = (BoundCompoundAssignmentExpression) base.RewriteCompoundAssignmentExpression(node);


            var result = Assignment(
                newNode.Variable,
                Binary(
                    Variable(newNode.Variable),
                    newNode.Operator,
                    newNode.Expression
                )
            );

            return result;
        }
    }
}