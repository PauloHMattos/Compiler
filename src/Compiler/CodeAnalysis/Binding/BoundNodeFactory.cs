using System.Collections.Immutable;
using System.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;

namespace Compiler.CodeAnalysis.Binding
{
    internal static class BoundNodeFactory
    {
        public static BoundNopStatement Nop(SyntaxNode syntax)
        {
            return new BoundNopStatement(syntax);
        }

        public static BoundLabelStatement Label(SyntaxNode syntax, BoundLabel label)
        {
            return new BoundLabelStatement(syntax, label);
        }

        public static BoundLiteralExpression Literal(SyntaxNode syntax, object literal)
        {
            Debug.Assert(literal is string || literal is bool || literal is int);

            return new BoundLiteralExpression(syntax, literal);
        }

        public static BoundBlockStatement Block(SyntaxNode syntax, params BoundStatement[] statements)
        {
            return new BoundBlockStatement(syntax, ImmutableArray.Create(statements));
        }

        public static BoundGotoStatement Goto(SyntaxNode syntax, BoundLabel label)
        {
            return new BoundGotoStatement(syntax, label);
        }

        public static BoundConditionalGotoStatement GotoTrue(SyntaxNode syntax, BoundLabel label, BoundExpression condition)
            => new BoundConditionalGotoStatement(syntax, label, condition, jumpIfTrue: true);


        public static BoundConditionalGotoStatement GotoFalse(SyntaxNode syntax, BoundLabel label, BoundExpression condition)
            => new BoundConditionalGotoStatement(syntax, label, condition, jumpIfTrue: false);

        public static BoundVariableExpression Variable(SyntaxNode syntax, BoundVariableDeclarationStatement variable, bool byReference)
        {
            return Variable(syntax, variable.Variable, byReference);
        }

        public static BoundVariableExpression Variable(SyntaxNode syntax, VariableSymbol variable, bool byReference)
        {
            return new BoundVariableExpression(syntax, variable, byReference);
        }

        public static BoundMemberAccessExpression Member(SyntaxNode syntax, BoundExpression instance, MemberSymbol member)
        {
            return new BoundMemberAccessExpression(syntax, instance, member);
        }

        public static BoundVariableDeclarationStatement VariableDeclaration(SyntaxNode syntax, VariableSymbol symbol, BoundExpression initializer)
        {
            return new BoundVariableDeclarationStatement(syntax, symbol, initializer);
        }

        public static BoundVariableDeclarationStatement VariableDeclaration(SyntaxNode syntax, string name, BoundExpression initializer)
            => VariableDeclarationInternal(syntax, name, initializer, isReadOnly: false);

        public static BoundVariableDeclarationStatement ConstantDeclaration(SyntaxNode syntax, string name, BoundExpression initializer)
            => VariableDeclarationInternal(syntax, name, initializer, isReadOnly: true);

        private static BoundVariableDeclarationStatement VariableDeclarationInternal(SyntaxNode syntax, string name, BoundExpression initializer, bool isReadOnly)
        {
            var local = new LocalVariableSymbol(name, isReadOnly, initializer.Type, initializer.ConstantValue);
            return new BoundVariableDeclarationStatement(syntax, local, initializer);
        }

        public static BoundAssignmentExpression Assignment(SyntaxNode syntax, BoundExpression left, BoundExpression expression)
        {
            return new BoundAssignmentExpression(syntax, left, expression);
        }

        public static BoundBinaryExpression Binary(SyntaxNode syntax, BoundExpression left, SyntaxKind kind, BoundExpression right)
        {
            var op = BoundBinaryOperator.Bind(kind, left.Type, right.Type);
            Debug.Assert(op != null);
            return Binary(syntax, left, op, right);
        }

        public static BoundBinaryExpression Binary(SyntaxNode syntax, BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            return new BoundBinaryExpression(syntax, left, op, right);
        }

        public static BoundBinaryExpression Add(SyntaxNode syntax, BoundExpression left, BoundExpression right)
            => Binary(syntax, left, SyntaxKind.PlusToken, right);
        public static BoundBinaryExpression LessOrEqual(SyntaxNode syntax, BoundExpression left, BoundExpression right)
            => Binary(syntax, left, SyntaxKind.LessOrEqualsToken, right);

        public static BoundWhileStatement While(SyntaxNode syntax, BoundExpression condition, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
        {
            return new BoundWhileStatement(syntax, condition, body, breakLabel, continueLabel);
        }

        public static BoundExpressionStatement Increment(SyntaxNode syntax, BoundVariableExpression variable)
        {
            return Increment(syntax, variable, Literal(syntax, 1));
        }

        public static BoundExpressionStatement Increment(SyntaxNode syntax, BoundVariableExpression variable, BoundExpression step)
        {
            var increment = Add(syntax, variable, step);
            var incrementAssign = new BoundAssignmentExpression(syntax, variable, increment);
            return new BoundExpressionStatement(syntax, incrementAssign);
        }

        public static BoundUnaryExpression Not(SyntaxNode syntax, BoundExpression condition)
        {
            Debug.Assert(condition.Type == TypeSymbol.Bool);
            var op = BoundUnaryOperator.Bind(SyntaxKind.BangToken, TypeSymbol.Bool);
            Debug.Assert(op != null);
            return new BoundUnaryExpression(syntax, condition, op);
        }
    }
}