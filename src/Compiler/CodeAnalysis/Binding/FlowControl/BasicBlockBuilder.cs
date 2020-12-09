using System;
using System.Collections.Generic;
using System.Linq;

namespace Compiler.CodeAnalysis.Binding.FlowControl
{
    internal sealed partial class ControlFlowGraph
    {
        internal sealed class BasicBlockBuilder
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

                        case BoundNodeKind.NopStatement:
                        case BoundNodeKind.SequencePointStatement:
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
    }
}