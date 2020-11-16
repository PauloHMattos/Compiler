﻿using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal sealed class BoundAssignmentExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
        public override TypeSymbol Type => Variable.Type;

        public BoundAssignmentExpression(SyntaxNode syntax,
                                         VariableSymbol variable,
                                         BoundExpression expression)
            : base(syntax)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}