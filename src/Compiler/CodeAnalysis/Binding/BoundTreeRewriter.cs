using System;
using System.Collections.Immutable;

namespace Compiler.CodeAnalysis.Binding
{
    internal abstract class BoundTreeRewriter
    {
        public BoundStatement RewriteStatement(BoundStatement statement)
        {
            switch (statement.Kind)
            {
                case BoundNodeKind.NopStatement:
                    return RewriteNopStatement((BoundNopStatement)statement);
                case BoundNodeKind.SequencePointStatement:
                    return RewriteSequencePointStatement((BoundSequencePointStatement)statement);
                case BoundNodeKind.BlockStatement:
                    return RewriteBlockStatement((BoundBlockStatement)statement);
                case BoundNodeKind.MemberBlockStatement:
                    return RewriteMemberBlockStatement((BoundMemberBlockStatement)statement);
                case BoundNodeKind.ExpressionStatement:
                    return RewriteExpressionStatement((BoundExpressionStatement)statement);
                case BoundNodeKind.VariableDeclarationStatement:
                    return RewriteVariableDeclarationStatement((BoundVariableDeclarationStatement)statement);
                case BoundNodeKind.IfStatement:
                    return RewriteIfStatement((BoundIfStatement)statement);
                case BoundNodeKind.DoWhileStatement:
                    return RewriteDoWhileStatement((BoundDoWhileStatement)statement);
                case BoundNodeKind.WhileStatement:
                    return RewriteWhileStatement((BoundWhileStatement)statement);
                case BoundNodeKind.ForStatement:
                    return RewriteForStatement((BoundForStatement)statement);
                case BoundNodeKind.LabelStatement:
                    return RewriteLabelStatement((BoundLabelStatement)statement);
                case BoundNodeKind.GotoStatement:
                    return RewriteGotoStatement((BoundGotoStatement)statement);
                case BoundNodeKind.ConditionalGotoStatement:
                    return RewriteConditionalGotoStatement((BoundConditionalGotoStatement)statement);
                case BoundNodeKind.ReturnStatement:
                    return RewriteReturnStatement((BoundReturnStatement)statement);
                default:
                    throw new InvalidOperationException($"Unexpected statement {statement.Kind}.");
            }
        }

        protected virtual BoundStatement RewriteNopStatement(BoundNopStatement node)
        {
            return node;
        }

        private BoundStatement RewriteSequencePointStatement(BoundSequencePointStatement node)
        {
            var statement = RewriteStatement(node.Statement);
            if (statement == node.Statement)
            {
                return node;
            }
            return new BoundSequencePointStatement(node.Syntax, statement, node.Location);
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = RewriteStatements(node.Statements);

            if (builder == null)
            {
                return node;
            }

            return new BoundBlockStatement(node.Syntax, builder.ToImmutable());
        }

        private ImmutableArray<BoundStatement>.Builder? RewriteStatements(ImmutableArray<BoundStatement> statements)
        {
            ImmutableArray<BoundStatement>.Builder? builder = null;

            for (var i = 0; i < statements.Length; i++)
            {
                var oldStatement = statements[i];
                var newStatement = RewriteStatement(oldStatement);
                if (newStatement != oldStatement && builder == null)
                {
                    builder = ImmutableArray.CreateBuilder<BoundStatement>(statements.Length);
                    builder.AddRange(statements, i);
                }
                builder?.Add(newStatement);
            }

            return builder;
        }
        
        private BoundStatement RewriteMemberBlockStatement(BoundMemberBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = RewriteStatements(node.Statements);
            if (builder == null)
            {
                return node;
            }
            return new BoundBlockStatement(node.Syntax, builder.ToImmutable());
        }


        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }
            return new BoundExpressionStatement(node.Syntax, expression);
        }

        protected virtual BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            var initializer = RewriteExpression(node.Initializer);
            if (initializer == node.Initializer)
            {
                return node;
            }
            return new BoundVariableDeclarationStatement(node.Syntax, node.Variable, initializer);
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var thenStatement = RewriteStatement(node.ThenStatement);
            var elseStatement = node.ElseStatement == null ? null : RewriteStatement(node.ElseStatement);
            if (condition == node.Condition &&
                thenStatement == node.ThenStatement &&
                elseStatement == node.ElseStatement)
            {
                return node;
            }
            return new BoundIfStatement(node.Syntax, condition, thenStatement, elseStatement);
        }

        protected virtual BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var body = RewriteStatement(node.Body);
            if (condition == node.Condition && body == node.Body)
            {
                return node;
            }
            return new BoundDoWhileStatement(node.Syntax, condition, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var body = RewriteStatement(node.Body);
            if (condition == node.Condition && body == node.Body)
            {
                return node;
            }
            return new BoundWhileStatement(node.Syntax, condition, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            var lowerBound = RewriteExpression(node.LowerBound);
            var upperBound = RewriteExpression(node.UpperBound);
            var step = RewriteExpression(node.Step);
            var body = RewriteStatement(node.Body);

            if (lowerBound == node.LowerBound && upperBound == node.UpperBound &&
                step == node.Step && body == node.Body)
            {
                return node;
            }
            return new BoundForStatement(node.Syntax, node.Variable, lowerBound, upperBound, step, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            if (condition == node.Condition)
            {
                return node;
            }
            return new BoundConditionalGotoStatement(node.Syntax, node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            var expression = node.Expression == null ? null : RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }
            return new BoundReturnStatement(node.Syntax, expression);
        }

        public BoundExpression RewriteExpression(BoundExpression expression)
        {
            switch (expression.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    return RewriteErrorExpression((BoundErrorExpression)expression);
                case BoundNodeKind.LiteralExpression:
                    return RewriteLiteralExpression((BoundLiteralExpression)expression);
                case BoundNodeKind.VariableExpression:
                    return RewriteVariableExpression((BoundVariableExpression)expression);
                case BoundNodeKind.AssignmentExpression:
                    return RewriteAssignmentExpression((BoundAssignmentExpression)expression);
                case BoundNodeKind.CompoundAssignmentExpression:
                    return RewriteCompoundAssignmentExpression((BoundCompoundAssignmentExpression)expression);
                case BoundNodeKind.UnaryExpression:
                    return RewriteUnaryExpression((BoundUnaryExpression)expression);
                case BoundNodeKind.BinaryExpression:
                    return RewriteBinaryExpression((BoundBinaryExpression)expression);
                case BoundNodeKind.CallExpression:
                    return RewriteCallExpression((BoundCallExpression)expression);
                case BoundNodeKind.ConversionExpression:
                    return RewriteConversionExpression((BoundConversionExpression)expression);
                case BoundNodeKind.MemberAccessExpression:
                    return RewriteMemberAccessExpression((BoundMemberAccessExpression)expression);
                case BoundNodeKind.SelfExpression:
                    return RewriteSelfExpression((BoundSelfExpression)expression);
                case BoundNodeKind.TypeReferenceExpression:
                    return RewriteTypeReferenceExpression((BoundTypeReferenceExpression)expression);
                default:
                    throw new InvalidOperationException($"Unexpected expression {expression.Kind}.");
            }
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            var expression = RewriteExpression(node.Right);
            if (expression == node)
            {
                return node;
            }
            return new BoundAssignmentExpression(node.Syntax, node.Left, expression);
        }

        protected virtual BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);
            if (left == node.Left && right == node.Right)
            {
                return node;
            }
            return new BoundCompoundAssignmentExpression(node.Syntax,
                                                         left,
                                                         node.Operator,
                                                         right);
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            var expression = RewriteExpression(node.Operand);
            if (expression == node)
            {
                return node;
            }
            return new BoundUnaryExpression(node.Syntax, expression, node.Operator);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);
            if (left == node.Left && right == node.Right)
            {
                return node;
            }
            return new BoundBinaryExpression(node.Syntax, left, node.Operator, right);
        }

        private BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }
            return new BoundConversionExpression(node.Syntax, node.Type, expression);
        }

        protected virtual BoundExpression RewriteMemberAccessExpression(BoundMemberAccessExpression node)
        {
            var expression = RewriteExpression(node.Instance);

            if (expression == node.Instance)
            {
                return node;
            }

            return new BoundMemberAccessExpression(expression.Syntax, expression, node.Member);
        }

        protected virtual BoundExpression RewriteTypeReferenceExpression(BoundTypeReferenceExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteSelfExpression(BoundSelfExpression node)
        {
            return node;
        }
    }
}