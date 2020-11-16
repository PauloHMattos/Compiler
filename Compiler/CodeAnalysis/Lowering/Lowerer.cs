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

        private static BoundBlockStatement RemoveDeadCode(BoundBlockStatement statement)
        {
            var controlFlow = ControlFlowGraph.Create(statement);
            var reachableStatements = new HashSet<BoundStatement>(controlFlow.Blocks.SelectMany(b => b.Statements));

            var builder = statement.Statements.ToBuilder();
            for (int i = builder.Count - 1; i >= 0; i--)
            {
                if (!reachableStatements.Contains(builder[i]))
                {
                    builder.RemoveAt(i);
                }
            }

            return new BoundBlockStatement(statement.Syntax, builder.ToImmutable());
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
                builder.Add(new BoundReturnStatement(statement.Syntax, null));
            }

            return new BoundBlockStatement(statement.Syntax, builder.ToImmutable());
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
                var endLabel = GenerateNewLabel();
                result = Block(
                    node.Syntax,
                    GotoFalse(node.Syntax, endLabel, node.Condition),
                    node.ThenStatement,
                    Label(node.Syntax, endLabel));
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
                result = Block(node.Syntax,
                                GotoFalse(node.Syntax, elseLabel, node.Condition),
                                node.ThenStatement,
                                Goto(node.Syntax, endLabel),
                                Label(node.Syntax, elseLabel),
                                node.ElseStatement,
                                Label(node.Syntax, endLabel));
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
            var result = Block(node.Syntax, 
                               Label(node.Syntax, bodyLabel),
                               node.Body,
                               Label(node.Syntax, node.ContinueLabel),
                               GotoTrue(node.Syntax, bodyLabel, node.Condition),
                               Label(node.Syntax, node.BreakLabel));

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

            var result = Block(node.Syntax, 
                               Goto(node.Syntax, node.ContinueLabel),
                               Label(node.Syntax, bodyLabel),
                               node.Body,
                               Label(node.Syntax, node.ContinueLabel),
                               GotoTrue(node.Syntax, bodyLabel, node.Condition),
                               Label(node.Syntax, node.BreakLabel));

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

            
            var lowerBound = VariableDeclaration(node.Syntax, node.Variable, node.LowerBound);
            // Use node.UpperBound.Syntax?
            var upperBound = ConstantDeclaration(node.Syntax, "upperBound", node.UpperBound);

            var result = Block(node.Syntax, 
                                lowerBound,
                                upperBound,
                                While(node.Syntax, 
                                    LessOrEqual(
                                    node.Syntax,
                                    Variable(node.Syntax, lowerBound),
                                    Variable(node.Syntax, upperBound)),
                                    Block(node.Syntax, 
                                        node.Body,
                                        Label(node.Syntax, node.ContinueLabel),
                                        Increment(
                                            node.Syntax, 
                                            Variable(node.Syntax, lowerBound), node.Step)),
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
                    return RewriteStatement(Goto(node.Syntax, node.Label));
                }
                else
                {
                    return RewriteStatement(Nop(node.Syntax));
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

            var newNode = (BoundCompoundAssignmentExpression)base.RewriteCompoundAssignmentExpression(node);


            var result = Assignment(
                newNode.Syntax, 
                newNode.Variable,
                Binary(
                    newNode.Syntax, 
                    Variable(newNode.Syntax, newNode.Variable),
                    newNode.Operator,
                    newNode.Expression
                )
            );

            return result;
        }
    }
}