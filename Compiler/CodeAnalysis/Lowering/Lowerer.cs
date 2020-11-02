﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
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

        public static BoundBlockStatement Lower(BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return Flatten(result);
        }

        private static BoundBlockStatement Flatten(BoundStatement statement)
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

            return new BoundBlockStatement(builder.ToImmutable());
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

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            // while <condition>
            //      <body>
            //
            // ---------------->
            //
            // goto check
            // continue:
            //      <body>
            // check:
            //      gotoTrue <condition> continue
            // end:

            var continueLabel = GenerateNewLabel();
            var checkLabel = GenerateNewLabel();
            var endLabel = GenerateNewLabel();

            var gotoCheck = new BoundGotoStatement(checkLabel);

            var continueLabelStatement = new BoundLabelStatement(continueLabel);
            var checkLabelStatement = new BoundLabelStatement(checkLabel);

            var gotoTrue = new BoundConditionalGotoStatement(continueLabel, node.Condition, true);

            var endLabelStatement = new BoundLabelStatement(endLabel);

            var result = new BoundBlockStatement(ImmutableArray.Create(
                gotoCheck,
                continueLabelStatement,
                node.Body,
                checkLabelStatement,
                gotoTrue,
                endLabelStatement
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
            //      <var> = <var> + <step>

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