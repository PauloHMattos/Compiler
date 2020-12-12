using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.CodeAnalysis.Diagnostics;

namespace Compiler.CodeAnalysis.Binding.FlowControl
{
    internal sealed partial class ControlFlowGraph
    {
        public sealed class GraphBuilder
        {
            private readonly Dictionary<BoundStatement, BasicBlock> _blockFromStatement;
            private readonly Dictionary<BoundLabel, BasicBlock> _blockFromLabel;
            private readonly List<BasicBlockBranch> _branches;
            private readonly BasicBlock _start;
            private readonly BasicBlock _end;
            private readonly DiagnosticBag _diagnosticBag;

            public GraphBuilder(DiagnosticBag diagnosticBag)
            {
                _blockFromStatement = new Dictionary<BoundStatement, BasicBlock>();
                _blockFromLabel = new Dictionary<BoundLabel, BasicBlock>();
                _branches = new List<BasicBlockBranch>();
                _start = new BasicBlock(true);
                _end = new BasicBlock(false);
                _diagnosticBag = diagnosticBag;
            }

            public ControlFlowGraph Build(List<BasicBlock> blocks)
            {
                if (blocks.Any())
                {
                    Connect(_start, blocks.First());
                }
                else
                {
                    Connect(_start, _end);
                }

                foreach (var block in blocks)
                {
                    RegisterLabels(block);
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    var current = blocks[i];
                    var next = i == blocks.Count - 1 ? _end : blocks[i + 1];
                    BuildBlock(current, next);
                }

                bool scan = true;
                while(scan)
                {
                    scan = false;
                    foreach (var block in blocks)
                    {
                        if (block.Incoming.Any())
                        {
                            continue;
                        }
                        RemoveBlock(blocks, block);
                        scan = true;
                        break;
                    }
                }

                blocks.Insert(0, _start);
                blocks.Add(_end);

                return new ControlFlowGraph(_start, _end, blocks, _branches);
            }

            private void BuildBlock(BasicBlock current, BasicBlock next)
            {
                foreach (var statement in current.Statements)
                {
                    var isLastStatementInBlock = statement == current.Statements.Last();
                    switch (statement.Kind)
                    {
                        case BoundNodeKind.GotoStatement:
                            var gs = (BoundGotoStatement)statement;
                            var toBlock = _blockFromLabel[gs.Label];
                            Connect(current, toBlock);
                            break;

                        case BoundNodeKind.ConditionalGotoStatement:
                            var cgs = (BoundConditionalGotoStatement)statement;
                            var thenBlock = _blockFromLabel[cgs.Label];
                            var elseBlock = next;
                            var negatedCondition = Negate(cgs.Condition);
                            var thenCondition = cgs.JumpIfTrue ? cgs.Condition : negatedCondition;
                            var elseCondition = cgs.JumpIfTrue ? negatedCondition : cgs.Condition;
                            Connect(current, thenBlock, thenCondition);
                            Connect(current, elseBlock, elseCondition);
                            break;

                        case BoundNodeKind.ReturnStatement:
                            Connect(current, _end);
                            break;

                        case BoundNodeKind.NopStatement:
                        case BoundNodeKind.SequencePointStatement:
                        case BoundNodeKind.VariableDeclarationStatement:
                        case BoundNodeKind.LabelStatement:
                        case BoundNodeKind.ExpressionStatement:
                            if (isLastStatementInBlock)
                            {
                                Connect(current, next);
                            }
                            break;

                        default:
                            throw new InvalidOperationException($"Unexpected statement: {statement.Kind}");
                    }
                }
            }

            private void RegisterLabels(BasicBlock block)
            {
                foreach (var statement in block.Statements)
                {
                    _blockFromStatement.Add(statement, block);
                    if (statement is BoundLabelStatement labelStatement)
                    {
                        _blockFromLabel.Add(labelStatement.Label, block);
                    }
                }
            }

            private void Connect(BasicBlock from, BasicBlock to, BoundExpression? condition = null)
            {
                if (condition?.ConstantValue != null)
                {
                    var value = (bool)condition.ConstantValue.Value;
                    if (!value)
                    {
                        return;
                    }
                    condition = null;
                }

                var branch = new BasicBlockBranch(from, to, condition);
                from.Outgoing.Add(branch);
                to.Incoming.Add(branch);
                _branches.Add(branch);
            }

            private void RemoveBlock(List<BasicBlock> blocks, BasicBlock block)
            {
                foreach (var branch in block.Incoming)
                {
                    branch.From.Outgoing.Remove(branch);
                    _branches.Remove(branch);
                }

                foreach (var branch in block.Outgoing)
                {
                    branch.To.Incoming.Remove(branch);
                    _branches.Remove(branch);
                }

                blocks.Remove(block);
                //_diagnosticBag.ReportUnreachableCode(block.Statements[0].Syntax.Location);
            }

            private static BoundExpression Negate(BoundExpression condition)
            {
                var negated = BoundNodeFactory.Not(condition.Syntax, condition);
                if (negated.ConstantValue != null)
                {
                    return new BoundLiteralExpression(condition.Syntax, negated.ConstantValue.Value);
                }
                return negated;
            }
        }
    }
}