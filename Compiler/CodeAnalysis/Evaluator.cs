using System;
using System.Collections.Generic;
using System.Diagnostics;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Symbols;

namespace Compiler.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundProgram _program;
        private readonly Dictionary<VariableSymbol, object> _globals;
        private readonly Stack<Dictionary<VariableSymbol, object>> _locals;
        private readonly Dictionary<FunctionSymbol, BoundBlockStatement> _functions;

        private object? _lastValue;

        public Evaluator(BoundProgram program, Dictionary<VariableSymbol, object> variables)
        {
            _program = program;
            _globals = variables;
            _locals = new Stack<Dictionary<VariableSymbol, object>>();
            _locals.Push(new Dictionary<VariableSymbol, object>());
            _functions = new Dictionary<FunctionSymbol, BoundBlockStatement>();

            var current = program;
            while (current != null)
            {
                foreach (var (function, body) in current.Functions)
                {
                    _functions.Add(function, body);
                }
                current = current.Previous;
            }
        }

        public object? Evaluate()
        {
            var function = _program.MainFunction ?? _program.ScriptFunction;
            if (function == null)
            {
                return null;
            }

            var body = _functions[function];
            return EvaluateStatement(body);
        }

        private object? EvaluateStatement(BoundBlockStatement body)
        {
            var labelToIndex = new Dictionary<BoundLabel, int>();
            for (var i = 0; i < body.Statements.Length; i++)
            {
                var statement = body.Statements[i];
                if (statement.Kind != BoundNodeKind.LabelStatement)
                {
                    continue;
                }

                var labelStatement = (BoundLabelStatement)statement;
                labelToIndex.Add(labelStatement.Label, i + 1);
            }

            var index = 0;
            while (index < body.Statements.Length)
            {
                var statement = body.Statements[index];
                switch (statement.Kind)
                {
                    case BoundNodeKind.NopStatement:
                        index++;
                        break;

                    case BoundNodeKind.ExpressionStatement:
                        EvaluateExpressionStatement((BoundExpressionStatement)statement);
                        index++;
                        break;

                    case BoundNodeKind.VariableDeclarationStatement:
                        EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)statement);
                        index++;
                        break;

                    case BoundNodeKind.LabelStatement:
                        index++;
                        break;

                    case BoundNodeKind.GotoStatement:
                        var gotoStatement = (BoundGotoStatement)statement;
                        index = labelToIndex[gotoStatement.Label];
                        break;

                    case BoundNodeKind.ConditionalGotoStatement:
                        index = EvaluateConditionalGotoStatement(labelToIndex, index, statement);
                        break;

                    case BoundNodeKind.ReturnStatement:
                        return EvaluateReturnStatement((BoundReturnStatement)statement);

                    default:
                        throw new InvalidOperationException($"Unexpected statement {statement.Kind}");
                }
            }
            return _lastValue;
        }

        private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement statement)
        {
            var value = EvaluateExpression(statement.Initializer);
            Debug.Assert(value != null);
            _lastValue = value;
            Assign(statement.Variable, value);
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement expressionStatement)
        {
            _lastValue = EvaluateExpression(expressionStatement.Expression);
        }

        private int EvaluateConditionalGotoStatement(Dictionary<BoundLabel, int> labelToIndex, int index, BoundStatement statement)
        {
            var conditionalGotoStatement = (BoundConditionalGotoStatement)statement;
            var condition = (bool)EvaluateExpression(conditionalGotoStatement.Condition)!;
            if (condition == conditionalGotoStatement.JumpIfTrue)
            {
                index = labelToIndex[conditionalGotoStatement.Label];
            }
            else
            {
                index++;
            }

            return index;
        }

        private object? EvaluateReturnStatement(BoundReturnStatement returnStatement)
        {
            _lastValue = returnStatement.Expression == null ?
                null :
                EvaluateExpression(returnStatement.Expression);
            return _lastValue;
        }

        private object? EvaluateExpression(BoundExpression expression)
        {
            if (expression.ConstantValue != null)
            {
                return EvaluateConstantExpression(expression);
            }

            switch (expression.Kind)
            {
                case BoundNodeKind.VariableExpression:
                    return EvaluateVariableExpression((BoundVariableExpression)expression);

                case BoundNodeKind.AssignmentExpression:
                    return EvaluateAssignmentExpression((BoundAssignmentExpression)expression);

                case BoundNodeKind.UnaryExpression:
                    return EvaluateUnaryExpression((BoundUnaryExpression)expression);

                case BoundNodeKind.BinaryExpression:
                    return EvaluateBinaryExpression((BoundBinaryExpression)expression);

                case BoundNodeKind.CallExpression:
                    return EvaluateCallExpression((BoundCallExpression)expression);

                case BoundNodeKind.ConversionExpression:
                    return EvaluateConversionExpression((BoundConversionExpression)expression);

                default:
                    throw new InvalidOperationException($"Unexpected expression {expression.Kind}");
            }
        }
            
        private static object EvaluateConstantExpression(BoundExpression n)
        {
            Debug.Assert(n.ConstantValue != null);
            return n.ConstantValue.Value;
        }

        private object EvaluateVariableExpression(BoundVariableExpression expression)
        {
            if (expression.Variable.Kind == SymbolKind.GlobalVariable)
            {
                return _globals[expression.Variable];
            }

            var locals = _locals.Peek();
            return locals[expression.Variable];
        }

        private object EvaluateAssignmentExpression(BoundAssignmentExpression assignmentExpression)
        {
            var value = EvaluateExpression(assignmentExpression.Expression);
            Debug.Assert(value != null);
            Assign(assignmentExpression.Variable, value);
            return value;
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression unaryExpression)
        {
            var operand = EvaluateExpression(unaryExpression.Operand);
            Debug.Assert(operand != null);

            switch (unaryExpression.Operator.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return (int)operand;
                case BoundUnaryOperatorKind.Negation:
                    return -(int)operand;
                case BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool)operand;
                case BoundUnaryOperatorKind.OnesComplement:
                    return ~(int)operand;
                default:
                    throw new InvalidOperationException($"Unexpected unary operator {unaryExpression.Operator.Kind}");
            }
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression binaryExpression)
        {
            var left = EvaluateExpression(binaryExpression.Left);
            var right = EvaluateExpression(binaryExpression.Right);
            Debug.Assert(left != null && right != null);

            switch (binaryExpression.Operator.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (binaryExpression.Type == TypeSymbol.Int)
                    {
                        return (int)left + (int)right;
                    }
                    else
                    {
                        return (string)left + (string)right;
                    }
                case BoundBinaryOperatorKind.Subtraction:
                    return (int)left - (int)right;
                case BoundBinaryOperatorKind.Multiplication:
                    return (int)left * (int)right;
                case BoundBinaryOperatorKind.Division:
                    return (int)left / (int)right;
                case BoundBinaryOperatorKind.Modulus:
                    return (int)left % (int)right;
                case BoundBinaryOperatorKind.LogicalAnd:
                    return (bool)left && (bool)right;
                case BoundBinaryOperatorKind.LogicalOr:
                    return (bool)left || (bool)right;
                case BoundBinaryOperatorKind.Equals:
                    return left.Equals(right);
                case BoundBinaryOperatorKind.NotEquals:
                    return !left.Equals(right);
                case BoundBinaryOperatorKind.Less:
                    return (int)left < (int)right;
                case BoundBinaryOperatorKind.LessOrEquals:
                    return (int)left <= (int)right;
                case BoundBinaryOperatorKind.Greater:
                    return (int)left > (int)right;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return (int)left >= (int)right;
                case BoundBinaryOperatorKind.BitwiseAnd:
                    if (binaryExpression.Type == TypeSymbol.Int)
                    {
                        return (int)left & (int)right;
                    }
                    else
                    {
                        return (bool)left & (bool)right;
                    }
                case BoundBinaryOperatorKind.BitwiseOr:
                    if (binaryExpression.Type == TypeSymbol.Int)
                    {
                        return (int)left | (int)right;
                    }
                    else
                    {
                        return (bool)left | (bool)right;
                    }
                case BoundBinaryOperatorKind.BitwiseXor:
                    if (binaryExpression.Type == TypeSymbol.Int)
                    {
                        return (int)left ^ (int)right;
                    }
                    else
                    {
                        return (bool)left ^ (bool)right;
                    }
                default:
                    throw new InvalidOperationException($"Unexpected binary operator {binaryExpression.Operator.Kind}");
            }
        }

        private object? EvaluateCallExpression(BoundCallExpression callExpression)
        {
            if (callExpression.Function == BuiltinFunctions.Input)
            {
                return Console.ReadLine();
            }

            if (callExpression.Function == BuiltinFunctions.Print)
            {
                var value = EvaluateExpression(callExpression.Arguments[0]);
                Console.WriteLine(value);
                return null;
            }

            var locals = new Dictionary<VariableSymbol, object>();
            for (int i = 0; i < callExpression.Arguments.Length; i++)
            {
                var parameter = callExpression.Function.Parameters[i];
                var value = EvaluateExpression(callExpression.Arguments[i]);
                Debug.Assert(value != null);
                locals.Add(parameter, value);
            }

            _locals.Push(locals);

            var statement = _functions[callExpression.Function];
            var result = EvaluateStatement(statement);

            _locals.Pop();

            return result;
        }

        private void Assign(VariableSymbol variable, object value)
        {
            if (variable.Kind == SymbolKind.GlobalVariable)
            {
                _globals[variable] = value;
            }
            else
            {
                var locals = _locals.Peek();
                locals[variable] = value;
            }
        }

        private object? EvaluateConversionExpression(BoundConversionExpression node)
        {
            var value = EvaluateExpression(node.Expression);
            if (node.Type == TypeSymbol.Any)
            {
                return value;
            }
            else if (node.Type == TypeSymbol.Bool)
            {
                return Convert.ToBoolean(value);
            }
            if (node.Type == TypeSymbol.Int)
            {
                return Convert.ToInt32(value);
            }
            if (node.Type == TypeSymbol.String)
            {
                return Convert.ToString(value);
            }
            throw new InvalidOperationException($"Unexpected type {node.Type}");
        }
    }
}