using System.Collections.Immutable;
using System.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal static class BoundNodeFactory
    {
        public static BoundNopStatement Nop()
        {
            return new BoundNopStatement();
        }

        public static BoundLabelStatement Label(BoundLabel label)
        {
            return new BoundLabelStatement(label);
        }

        public static BoundLiteralExpression Literal(object literal)
        {
            Debug.Assert(literal is string || literal is bool || literal is int);

            return new BoundLiteralExpression(literal);
        }

        public static BoundBlockStatement Block(params BoundStatement[] statements)
        {
            return new BoundBlockStatement(ImmutableArray.Create(statements));
        }

        public static BoundGotoStatement Goto(BoundLabelStatement label)
        {
            return new BoundGotoStatement(label.Label);
        }

        public static BoundGotoStatement Goto(BoundLabel label)
        {
            return new BoundGotoStatement(label);
        }

        public static BoundConditionalGotoStatement GotoIf(BoundLabelStatement label, BoundExpression condition, bool jumpIfTrue)
        {
            return new BoundConditionalGotoStatement(label.Label, condition, jumpIfTrue);
        }

        public static BoundConditionalGotoStatement GotoTrue(BoundLabelStatement label, BoundExpression condition)
            => GotoIf(label, condition, jumpIfTrue: true);

        public static BoundConditionalGotoStatement GotoFalse(BoundLabelStatement label, BoundExpression condition)
            => GotoIf(label, condition, jumpIfTrue: false);

        public static BoundVariableExpression Variable(BoundVariableDeclarationStatement variable)
        {
            return Variable(variable.Variable);
        }
        
        public static BoundVariableExpression Variable(VariableSymbol variable)
        {
            return new BoundVariableExpression(variable);
        }

        public static BoundVariableDeclarationStatement VariableDeclaration(VariableSymbol symbol, BoundExpression initializer)
        {
            return new BoundVariableDeclarationStatement(symbol, initializer);
        }

        public static BoundVariableDeclarationStatement VariableDeclaration(string name, BoundExpression initializer)
            => VariableDeclarationInternal(name, initializer, isReadOnly: false);

        public static BoundVariableDeclarationStatement ConstantDeclaration(string name, BoundExpression initializer)
            => VariableDeclarationInternal(name, initializer, isReadOnly: true);

        private static BoundVariableDeclarationStatement VariableDeclarationInternal(string name, BoundExpression initializer, bool isReadOnly)
        {
            var local = new LocalVariableSymbol(name, isReadOnly, initializer.Type, initializer.ConstantValue);
            return new BoundVariableDeclarationStatement(local, initializer);
        }

        public static BoundAssignmentExpression Assignment(VariableSymbol variable, BoundExpression expression)
        {
            return new BoundAssignmentExpression(variable, expression);
        }

        public static BoundBinaryExpression Binary(BoundExpression left, SyntaxKind kind, BoundExpression right)
        {
            var op = BoundBinaryOperator.Bind(kind, left.Type, right.Type);
            Debug.Assert(op != null);
            return Binary(left, op, right);
        }
        
        public static BoundBinaryExpression Binary(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            return new BoundBinaryExpression(left, op, right);
        }

        public static BoundBinaryExpression Add(BoundExpression left, BoundExpression right)
            => Binary(left, SyntaxKind.PlusToken, right);
        public static BoundBinaryExpression LessOrEqual(BoundExpression left, BoundExpression right)
            => Binary(left, SyntaxKind.LessOrEqualsToken, right);

        public static BoundWhileStatement While(BoundExpression condition, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
        {
            return new BoundWhileStatement(condition, body, breakLabel, continueLabel);
        }

        public static BoundExpressionStatement Increment(BoundVariableExpression variable)
        {
            var increment = Add(variable, Literal(1));
            var incrementAssign = new BoundAssignmentExpression(variable.Variable, increment);
            return new BoundExpressionStatement(incrementAssign);
        }
        
        public static BoundExpressionStatement Increment(BoundVariableExpression variable, BoundExpression step)
        {
            var increment = Add(variable, step);
            var incrementAssign = new BoundAssignmentExpression(variable.Variable, increment);
            return new BoundExpressionStatement(incrementAssign);
        }
    }
}