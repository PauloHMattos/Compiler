using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace Compiler.CodeAnalysis.Binding.FlowControl
{
    internal sealed partial class ControlFlowGraph
    {
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
                {
                    return "<Start>";
                }

                if (IsEnd)
                {
                    return "<End>";
                }

                using var writer = new StringWriter();
                using var identedWriter = new IndentedTextWriter(writer);
                foreach (var statement in Statements)
                {
                    statement.WriteTo(identedWriter);
                }
                return writer.ToString();
            }
        }
    }
}