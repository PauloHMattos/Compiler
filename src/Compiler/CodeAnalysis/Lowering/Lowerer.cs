using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Binding.FlowControl;
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

        public static BoundBlockStatement Lower(Symbol symbol, BoundStatement statement)
        {
            if (symbol is not FunctionSymbol)
            {
                throw new InvalidOperationException($"Symbol of type {symbol.Kind} not expected in Lowerer.");
            }

            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return RemoveDeadCode(Flatten(symbol, result));
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

        private static BoundBlockStatement Flatten(Symbol symbol, BoundStatement statement)
        {
            // TODO: Take into account nested scopes when Flattening.  The compiler allows a naming collision
            // to occur if separate scope blocks contain identically named symbols.

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

            if (symbol is FunctionSymbol function && function.ReturnType == TypeSymbol.Void &&
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
            var lowerBound = VariableDeclaration(node.LowerBound.Syntax, node.Variable, node.LowerBound);
            // Use node.UpperBound.Syntax?
            var upperBound = ConstantDeclaration(node.UpperBound.Syntax, "upperBound", node.UpperBound);

            var result = Block(node.Syntax, 
                                lowerBound,
                                upperBound,
                                While(node.Syntax, 
                                    LessOrEqual(
                                    node.Syntax,
                                    Variable(node.Syntax, lowerBound, false),
                                    Variable(node.Syntax, upperBound, false)),
                                    Block(node.Syntax, 
                                        node.Body,
                                        Label(node.Syntax, node.ContinueLabel),
                                        Increment(
                                            node.Syntax, 
                                            Variable(node.Syntax, lowerBound, false), node.Step)),
                                    node.BreakLabel,
                                    continueLabel: GenerateNewLabel())
            );

            return RewriteStatement(result);
        }

        protected override BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            // a <op>= b
            //
            // --->
            //
            // a = (a <op> b)

            var newNode = (BoundCompoundAssignmentExpression)base.RewriteCompoundAssignmentExpression(node);


            return Assignment(
                newNode.Syntax, 
                newNode.Left,
                Binary(
                    newNode.Syntax, 
                    newNode.Left,
                    newNode.Operator,
                    newNode.Right
                )
            );
        }

        protected override BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            var rewrittenNode = base.RewriteExpressionStatement(node);
            return new BoundSequencePointStatement(rewrittenNode.Syntax, rewrittenNode, rewrittenNode.Syntax.Location);
        }

        protected override BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            var rewrittenNode = base.RewriteVariableDeclarationStatement(node);
            return new BoundSequencePointStatement(rewrittenNode.Syntax, rewrittenNode, rewrittenNode.Syntax.Location);
        }
        
        protected override BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            var rewrittenNode = base.RewriteReturnStatement(node);
            return new BoundSequencePointStatement(rewrittenNode.Syntax, rewrittenNode, rewrittenNode.Syntax.Location);
        }
    }
}