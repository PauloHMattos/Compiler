using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compiler.CodeAnalysis.Diagnostics;

namespace Compiler.CodeAnalysis.Binding.FlowControl
{
    internal sealed partial class ControlFlowGraph
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

        public static ControlFlowGraph Create(BoundBlockStatement body, DiagnosticBag diagnostics)
        {
            var basicBlockBuilder = new BasicBlockBuilder();
            var blocks = basicBlockBuilder.Build(body);

            var graphBuilder = new GraphBuilder(diagnostics);
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStatement body, DiagnosticBag diagnostics)
        {
            var graph = Create(body, diagnostics);

            foreach (var branch in graph.End.Incoming)
            {
                var lastStatement = branch.From.Statements.LastOrDefault();
                if (lastStatement is BoundSequencePointStatement s)
                {
                    lastStatement = s.Statement;
                }
                if (lastStatement == null || lastStatement.Kind != BoundNodeKind.ReturnStatement)
                {
                    return false;
                }
            }

            return true;
        }
    }
}