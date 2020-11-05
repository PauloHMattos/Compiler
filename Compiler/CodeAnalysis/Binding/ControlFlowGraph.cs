using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class ControlFlowGraph
    {
        public BasicBlock Start { get; }
        public BasicBlock End { get; }
        public List<BasicBlock> Blocks { get; }
        public List<BasicBlockBranch> Branches { get; }

        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> blocks, List<BasicBlockBranch> branches)
        {
            Start = start;
            End = end;
            Blocks = blocks;
            Branches = branches;
        }

        public sealed class BasicBlock
        {
            public bool IsStart { get; }
            public bool IsEnd { get; }
            public List<BoundStatement> Statements { get; }
            public List<BasicBlockBranch> Incoming { get; }
            public List<BasicBlockBranch> Outgoing { get; }

            public BasicBlock()
            {
                Statements = new List<BoundStatement>();
                Incoming = new List<BasicBlockBranch>();
                Outgoing = new List<BasicBlockBranch>();
            }

            public BasicBlock(bool isStart)
                : this()
            {
                IsStart = isStart;
                IsEnd = !isStart;
            }


            public override string ToString()
            {
                if (IsStart)
                    return "<Start>";

                if (IsEnd)
                    return "<End>";

                using (var writer = new StringWriter())
                using (var identedWriter = new IndentedTextWriter(writer))
                {
                    foreach (var statement in Statements)
                    {
                        statement.WriteTo(identedWriter);
                    }
                    return writer.ToString();
                }
            }
        }

        public sealed class BasicBlockBranch
        {
            public BasicBlock From { get; }
            public BasicBlock To { get; }
            public BoundExpression Condition { get; }

            public BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpression condition)
            {
                From = from;
                To = to;
                Condition = condition;
            }

            public override string ToString()
            {
                if (Condition == null)
                    return string.Empty;

                return Condition.ToString();
            }
        }

        public sealed class BasicBlockBuilder
        {
            private readonly List<BoundStatement> _statements;
            private readonly List<BasicBlock> _blocks;

            public BasicBlockBuilder()
            {
                _statements = new List<BoundStatement>();
                _blocks = new List<BasicBlock>();
            }

            public List<BasicBlock> Build(BoundBlockStatement block)
            {
                foreach (var statement in block.Statements)
                {
                    switch (statement.Kind)
                    {
                        case BoundNodeKind.LabelStatement:
                            StartBlock();
                            _statements.Add(statement);
                            break;
                        case BoundNodeKind.GotoStatement:
                        case BoundNodeKind.ConditionalGotoStatement:
                        case BoundNodeKind.ReturnStatement:
                            _statements.Add(statement);
                            StartBlock();
                            break;
                        case BoundNodeKind.VariableDeclarationStatement:
                        case BoundNodeKind.ExpressionStatement:
                            _statements.Add(statement);
                            break;
                        default:
                            throw new InvalidOperationException($"Unexpected statement: {statement.Kind}");
                    }
                }

                EndBlock();

                return _blocks.ToList();
            }

            private void StartBlock()
            {
                EndBlock();
            }

            private void EndBlock()
            {
                if (_statements.Count > 0)
                {
                    var block = new BasicBlock();
                    block.Statements.AddRange(_statements);
                    _blocks.Add(block);
                    _statements.Clear();
                }
            }
        }

        public sealed class GraphBuilder
        {
            private readonly Dictionary<BoundStatement, BasicBlock> _blockFromStatement;
            private readonly Dictionary<BoundLabel, BasicBlock> _blockFromLabel;
            private readonly List<BasicBlockBranch> _branches;
            private readonly BasicBlock _start;
            private readonly BasicBlock _end;

            public GraphBuilder()
            {
                _blockFromStatement = new Dictionary<BoundStatement, BasicBlock>();
                _blockFromLabel = new Dictionary<BoundLabel, BasicBlock>();
                _branches = new List<BasicBlockBranch>();
                _start = new BasicBlock(true);
                _end = new BasicBlock(false);
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
                    foreach (var statement in block.Statements)
                    {
                        _blockFromStatement.Add(statement, block);
                        if (statement is BoundLabelStatement labelStatement)
                        {
                            _blockFromLabel.Add(labelStatement.Label, block);
                        }
                    }
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    var current = blocks[i];
                    var next = i == blocks.Count - 1 ? _end : blocks[i + 1];

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

            ScanAgain:
                foreach (var block in blocks)
                {
                    if (block.Incoming.Any())
                    {
                        continue;
                    }
                    RemoveBlock(blocks, block);
                    goto ScanAgain;
                }

                blocks.Insert(0, _start);
                blocks.Add(_end);

                return new ControlFlowGraph(_start, _end, blocks, _branches);
            }

            private void Connect(BasicBlock from, BasicBlock to, BoundExpression condition = null)
            {
                if (condition is BoundLiteralExpression l)
                {
                    var value = (bool)l.Value;
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
            }

            private BoundExpression Negate(BoundExpression condition)
            {
                if (condition is BoundLiteralExpression literal)
                {
                    var value = (bool)literal.Value;
                    return new BoundLiteralExpression(!value);
                }

                var op = BoundUnaryOperator.Bind(SyntaxKind.BangToken, TypeSymbol.Bool);
                return new BoundUnaryExpression(condition, op);
            }
        }

        public void WriteTo(TextWriter writer)
        {
            string Quote(string text)
            {
                return "\"" + text.TrimEnd().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\l") + "\"";
            }

            writer.WriteLine("digraph G {");

            var blockIds = new Dictionary<BasicBlock, string>();

            for (int i = 0; i < Blocks.Count; i++)
            {
                var id = $"N{i}";
                blockIds.Add(Blocks[i], id);
            }

            foreach (var block in Blocks)
            {
                var id = blockIds[block];
                var label = Quote(block.ToString());
                writer.WriteLine($"    {id} [label = {label}, shape = box]");
            }

            foreach (var branch in Branches)
            {
                var fromId = blockIds[branch.From];
                var toId = blockIds[branch.To];
                var label = Quote(branch.ToString());
                writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
            }

            writer.WriteLine("}");
        }

        public static ControlFlowGraph Create(BoundBlockStatement body)
        {
            var basicBlockBuilder = new BasicBlockBuilder();
            var blocks = basicBlockBuilder.Build(body);

            var graphBuilder = new GraphBuilder();
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStatement body)
        {
            var graph = Create(body);

            foreach (var branch in graph.End.Incoming)
            {
                var lastStatement = branch.From.Statements.LastOrDefault();
                if (lastStatement == null || lastStatement.Kind != BoundNodeKind.ReturnStatement)
                {
                    return false;
                }
            }

            return true;
        }
    }
}