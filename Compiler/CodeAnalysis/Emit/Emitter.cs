using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Compiler.CodeAnalysis.Emit
{
    internal class Emitter
    {
        private readonly DiagnosticBag _diagnostics;
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly TypeDefinition _typeDefinition;
        private readonly List<AssemblyDefinition> _assemblies;
        private readonly Dictionary<TypeSymbol, TypeReference> _resolvedTypes;
        private readonly Dictionary<FunctionSymbol, MethodDefinition> _methods;
        private readonly Dictionary<VariableSymbol, VariableDefinition> _locals;
        private readonly Dictionary<BoundLabel, int> _labels;
        private readonly List<Fixup> _fixups;

        private MethodReference _objectEqualsReference;
        private MethodReference _consoleReadLineReference;
        private MethodReference _consoleWriteLineReference;
        private MethodReference _stringConcatReference;
        private MethodReference _convertToBooleanReference;
        private MethodReference _convertToInt32Reference;
        private MethodReference _convertToStringReference;

        private Emitter(string moduleName, IEnumerable<string> references)
        {
            _diagnostics = new DiagnosticBag();
            _assemblies = new List<AssemblyDefinition>();
            _resolvedTypes = new Dictionary<TypeSymbol, TypeReference>();
            _methods = new Dictionary<FunctionSymbol, MethodDefinition>();
            _locals = new Dictionary<VariableSymbol, VariableDefinition>();

            _labels = new Dictionary<BoundLabel, int>();
            _fixups = new List<Fixup>();

            var assemblyName = new AssemblyNameDefinition(moduleName, new Version("1.0"));
            _assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);

            ReadAssemblies(references);
            ResolveTypes();
            ResolveMethods();

            var objectType = Import(TypeSymbol.Any);
            _typeDefinition = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, objectType);
            _assemblyDefinition.MainModule.Types.Add(_typeDefinition);
        }

        private void ResolveMethods()
        {
            _objectEqualsReference = ResolveMethod<object>("Equals", new[] { typeof(object), typeof(object) });
            _consoleReadLineReference = ResolveMethod(typeof(Console), "ReadLine", Array.Empty<Type>());
            _consoleWriteLineReference = ResolveMethod(typeof(Console), "WriteLine", new[] { typeof(object) });
            _stringConcatReference = ResolveMethod<string>("Concat", new[] { typeof(string), typeof(string) });
            _convertToBooleanReference = ResolveMethod(typeof(Convert), "ToBoolean", new[] { typeof(object) });
            _convertToInt32Reference = ResolveMethod(typeof(Convert), "ToInt32", new[] { typeof(object) });
            _convertToStringReference = ResolveMethod(typeof(Convert), "ToString", new[] { typeof(object) });
        }

        private MethodReference ResolveMethod<T>(string methodName, Type[] parameterTypes)
        {
            return ResolveMethod(typeof(T), methodName, parameterTypes);
        }

        private MethodReference ResolveMethod(Type type, string methodName, Type[] parameterTypes)
        {
            var typeName = type.FullName;
            var foundTypes = _assemblies.SelectMany(a => a.Modules)
                                       .SelectMany(m => m.Types)
                                       .Where(t => t.FullName == typeName)
                                       .ToArray();

            if (foundTypes.Length == 0)
            {
                _diagnostics.ReportRequiredTypeNotFound(null, typeName);
                return null;
            }
            else if (foundTypes.Length > 1)
            {
                _diagnostics.ReportRequiredTypeAmbiguous(null, typeName, foundTypes);
                return null;
            }

            var foundType = foundTypes[0];
            var methods = foundType.Methods.Where(m => m.Name == methodName);

            foreach (var method in methods)
            {
                if (method.Parameters.Count != parameterTypes.Length)
                {
                    continue;
                }

                var allParametersMatch = true;

                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    var parameterTypeName = parameterTypes[i].FullName;
                    if (method.Parameters[i].ParameterType.FullName == parameterTypeName)
                    {
                        continue;
                    }
                    allParametersMatch = false;
                    break;
                }

                if (!allParametersMatch)
                {
                    continue;
                }

                return _assemblyDefinition.MainModule.ImportReference(method);
            }

            _diagnostics.ReportRequiredMethodNotFound(typeName, methodName, parameterTypes);
            return null;
        }
        private void ReadAssemblies(IEnumerable<string> references)
        {
            foreach (var reference in references)
            {
                Console.WriteLine(reference);
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(reference);
                    _assemblies.Add(assembly);
                }
                catch (BadImageFormatException)
                {
                    _diagnostics.ReportInvalidReference(reference);
                }
            }
        }

        private void ResolveTypes()
        {
            foreach (var type in TypeSymbol.GetBuiltInTypes())
            {
                var metadataName = type.NetType.FullName;
                var foundTypes = _assemblies.SelectMany(a => a.Modules)
                                            .SelectMany(a => a.Types)
                                            .Where(t => t.FullName == metadataName)
                                            .ToArray();

                if (foundTypes.Length == 1)
                {
                    var typeReference = _assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                    _resolvedTypes.Add(type, typeReference);
                }
                else if (foundTypes.Length == 0)
                {
                    _diagnostics.ReportRequiredTypeNotFound(type.Name, metadataName);
                }
                else if (foundTypes.Length > 1)
                {
                    _diagnostics.ReportRequiredTypeAmbiguous(type.Name, metadataName, foundTypes);
                }
            }
        }

        internal static ImmutableArray<Diagnostic> Emit(
            BoundProgram program,
            string moduleName,
            IEnumerable<string> references,
            string outputPath)
        {
            if (program.Diagnostics.Any())
            {
                return program.Diagnostics;
            }
            var emitter = new Emitter(moduleName, references);
            return emitter.Emit(program, outputPath);
        }

        private ImmutableArray<Diagnostic> Emit(BoundProgram program, string outputPath)
        {
            if (_diagnostics.Any())
            {
                return _diagnostics.ToImmutableArray();
            }

            EmitFunctionDeclarations(program.Functions);

            if (program.MainFunction != null)
            {
                _assemblyDefinition.EntryPoint = _methods[program.MainFunction];
            }

            _assemblyDefinition.Write(outputPath);
            return _diagnostics.ToImmutableArray();
        }

        private void EmitFunctionDeclarations(ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions)
        {
            foreach (var (function, _) in functions)
            {
                EmitFunctionDeclaration(function);
            }
            foreach (var (function, body) in functions)
            {
                EmitFunctionBody(function, body);
            }
        }

        private TypeReference Import(TypeSymbol type)
        {
            return _resolvedTypes[type];
        }

        private void EmitFunctionDeclaration(FunctionSymbol function)
        {
            var functionType = Import(function.Type);
            var method = new MethodDefinition(function.Name, MethodAttributes.Static | MethodAttributes.Private, functionType);

            foreach (var parameter in function.Parameters)
            {
                var parameterType = Import(parameter.Type);
                var parameterAttributes = ParameterAttributes.None;
                var parameterDefinition = new ParameterDefinition(parameter.Name, parameterAttributes, parameterType);
                method.Parameters.Add(parameterDefinition);
            }

            _typeDefinition.Methods.Add(method);
            _methods.Add(function, method);
        }

        private void EmitFunctionBody(FunctionSymbol function, BoundBlockStatement body)
        {
            var method = _methods[function];
            _locals.Clear();
            _labels.Clear();
            _fixups.Clear();

            var ilProcessor = method.Body.GetILProcessor();

            foreach (var statement in body.Statements)
            {
                EmitStatement(ilProcessor, statement);
            }

            foreach (var fixup in _fixups)
            {
                var targetLabel = fixup.Target;
                var targetInstructionIndex = _labels[targetLabel];
                var targetInstruction = ilProcessor.Body.Instructions[targetInstructionIndex];
                var instructionToFixup = ilProcessor.Body.Instructions[fixup.InstructionIndex];
                instructionToFixup.Operand = targetInstruction;
            }

            method.Body.Optimize();
        }

        private void EmitStatement(ILProcessor ilProcessor, BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.NopStatement:
                    EmitNopStatement(ilProcessor, (BoundNopStatement)node);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    EmitVariableDeclaration(ilProcessor, (BoundVariableDeclarationStatement)node);
                    break;
                case BoundNodeKind.LabelStatement:
                    EmitLabelStatement(ilProcessor, (BoundLabelStatement)node);
                    break;
                case BoundNodeKind.GotoStatement:
                    EmitGotoStatement(ilProcessor, (BoundGotoStatement)node);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    EmitConditionalGotoStatement(ilProcessor, (BoundConditionalGotoStatement)node);
                    break;
                case BoundNodeKind.ReturnStatement:
                    EmitReturnStatement(ilProcessor, (BoundReturnStatement)node);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EmitExpressionStatement(ilProcessor, (BoundExpressionStatement)node);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitNopStatement(ILProcessor ilProcessor, BoundNopStatement node)
        {
            ilProcessor.Emit(OpCodes.Nop);
        }

        private void EmitVariableDeclaration(ILProcessor ilProcessor, BoundVariableDeclarationStatement node)
        {
            var typeReference = Import(node.Variable.Type);
            var variableDefinition = new VariableDefinition(typeReference);
            _locals.Add(node.Variable, variableDefinition);
            ilProcessor.Body.Variables.Add(variableDefinition);

            EmitExpression(ilProcessor, node.Initializer);
            ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
        }

        private void EmitLabelStatement(ILProcessor ilProcessor, BoundLabelStatement node)
        {
            _labels.Add(node.Label, ilProcessor.Body.Instructions.Count);
        }

        private void EmitGotoStatement(ILProcessor ilProcessor, BoundGotoStatement node)
        {
            _fixups.Add(new Fixup(ilProcessor.Body.Instructions.Count, node.Label));
            ilProcessor.Emit(OpCodes.Br, Instruction.Create(OpCodes.Nop));
        }

        private void EmitConditionalGotoStatement(ILProcessor ilProcessor, BoundConditionalGotoStatement node)
        {
            EmitExpression(ilProcessor, node.Condition);

            var opCode = node.JumpIfTrue ? OpCodes.Brtrue : OpCodes.Brfalse;
            _fixups.Add(new Fixup(ilProcessor.Body.Instructions.Count, node.Label));
            ilProcessor.Emit(opCode, Instruction.Create(OpCodes.Nop));
        }

        private void EmitReturnStatement(ILProcessor ilProcessor, BoundReturnStatement node)
        {
            if (node.Expression != null)
            {

                EmitExpression(ilProcessor, node.Expression);
            }
            ilProcessor.Emit(OpCodes.Ret);
        }

        private void EmitExpressionStatement(ILProcessor ilProcessor, BoundExpressionStatement node)
        {
            EmitExpression(ilProcessor, node.Expression);

            if (node.Expression.Type != TypeSymbol.Void)
            {
                ilProcessor.Emit(OpCodes.Pop);
            }
        }


        private void EmitExpression(ILProcessor ilProcessor, BoundExpression node)
        {
            if (node.ConstantValue != null)
            {
                EmitConstantExpression(ilProcessor, node);
                return;
            }

            switch (node.Kind)
            {
                case BoundNodeKind.VariableExpression:
                    EmitVariableExpression(ilProcessor, (BoundVariableExpression)node);
                    break;
                case BoundNodeKind.AssignmentExpression:
                    EmitAssignmentExpression(ilProcessor, (BoundAssignmentExpression)node);
                    break;
                case BoundNodeKind.UnaryExpression:
                    EmitUnaryExpression(ilProcessor, (BoundUnaryExpression)node);
                    break;
                case BoundNodeKind.BinaryExpression:
                    EmitBinaryExpression(ilProcessor, (BoundBinaryExpression)node);
                    break;
                case BoundNodeKind.CallExpression:
                    EmitCallExpression(ilProcessor, (BoundCallExpression)node);
                    break;
                case BoundNodeKind.ConversionExpression:
                    EmitConversionExpression(ilProcessor, (BoundConversionExpression)node);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitConstantExpression(ILProcessor ilProcessor, BoundExpression node)
        {
            if (node.Type == TypeSymbol.Bool)
            {
                var value = (bool)node.ConstantValue.Value;
                var instruction = value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
                ilProcessor.Emit(instruction);
            }
            else if (node.Type == TypeSymbol.Int)
            {
                var value = (int)node.ConstantValue.Value;
                ilProcessor.Emit(OpCodes.Ldc_I4, value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                var value = (string)node.ConstantValue.Value;
                ilProcessor.Emit(OpCodes.Ldstr, value);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected constant expression type: {node.Type}");
            }
        }

        private void EmitVariableExpression(ILProcessor ilProcessor, BoundVariableExpression node)
        {
            if (node.Variable is ParameterSymbol parameter)
            {
                ilProcessor.Emit(OpCodes.Ldarg, parameter.Ordinal);
            }
            else
            {
                var variableDefinition = _locals[node.Variable];
                ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
            }
        }

        private void EmitAssignmentExpression(ILProcessor ilProcessor, BoundAssignmentExpression node)
        {
            var variableDefinition = _locals[node.Variable];
            EmitExpression(ilProcessor, node.Expression);
            ilProcessor.Emit(OpCodes.Dup);
            ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
        }

        private void EmitUnaryExpression(ILProcessor ilProcessor, BoundUnaryExpression node)
        {
            EmitExpression(ilProcessor, node.Operand);

            if (node.Operator.Kind == BoundUnaryOperatorKind.Identity)
            {
                // Done
            }
            else if (node.Operator.Kind == BoundUnaryOperatorKind.LogicalNegation)
            {
                ilProcessor.Emit(OpCodes.Ldc_I4_0);
                ilProcessor.Emit(OpCodes.Ceq);
            }
            else if (node.Operator.Kind == BoundUnaryOperatorKind.Negation)
            {
                ilProcessor.Emit(OpCodes.Neg);
            }
            else if (node.Operator.Kind == BoundUnaryOperatorKind.OnesComplement)
            {
                ilProcessor.Emit(OpCodes.Not);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected unary operator {SyntaxFacts.GetText(node.Operator.SyntaxKind)}({node.Operand.Type})");
            }
        }

        private void EmitBinaryExpression(ILProcessor ilProcessor, BoundBinaryExpression node)
        {
            EmitExpression(ilProcessor, node.Left);
            EmitExpression(ilProcessor, node.Right);

            // +(string, string)

            if (node.Operator.Kind == BoundBinaryOperatorKind.Addition
                && node.Left.Type == TypeSymbol.String
                && node.Right.Type == TypeSymbol.String)
            {
                ilProcessor.Emit(OpCodes.Call, _stringConcatReference);
                return;
            }

            // ==(any, any)
            // ==(string, string)

            if (node.Operator.Kind == BoundBinaryOperatorKind.Equals && 
                (node.Left.Type == TypeSymbol.Any && node.Right.Type == TypeSymbol.Any ||
                node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String))
            {
                ilProcessor.Emit(OpCodes.Call, _objectEqualsReference);
                return;
            }

            // !=(any, any)
            // !=(string, string)

            if (node.Operator.Kind == BoundBinaryOperatorKind.NotEquals && 
                (node.Left.Type == TypeSymbol.Any && node.Right.Type == TypeSymbol.Any ||
                node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String))
            {
                ilProcessor.Emit(OpCodes.Call, _objectEqualsReference);
                ilProcessor.Emit(OpCodes.Ldc_I4_0);
                ilProcessor.Emit(OpCodes.Ceq);
                return;
            }

            switch (node.Operator.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    ilProcessor.Emit(OpCodes.Add);
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                    ilProcessor.Emit(OpCodes.Sub);
                    break;
                case BoundBinaryOperatorKind.Multiplication:
                    ilProcessor.Emit(OpCodes.Mul);
                    break;
                case BoundBinaryOperatorKind.Division:
                    ilProcessor.Emit(OpCodes.Div);
                    break;
                case BoundBinaryOperatorKind.Modulus:
                    ilProcessor.Emit(OpCodes.Rem);
                    break;
                // TODO: Implement short-circuit evaluation
                case BoundBinaryOperatorKind.LogicalAnd:
                case BoundBinaryOperatorKind.BitwiseAnd:
                    ilProcessor.Emit(OpCodes.And);
                    break;
                // TODO: Implement short-circuit evaluation
                case BoundBinaryOperatorKind.LogicalOr:
                case BoundBinaryOperatorKind.BitwiseOr:
                    ilProcessor.Emit(OpCodes.Or);
                    break;
                case BoundBinaryOperatorKind.BitwiseXor:
                    ilProcessor.Emit(OpCodes.Xor);
                    break;
                case BoundBinaryOperatorKind.Equals:
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    ilProcessor.Emit(OpCodes.Ceq);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.Less:
                    ilProcessor.Emit(OpCodes.Clt);
                    break;
                case BoundBinaryOperatorKind.LessOrEquals:
                    ilProcessor.Emit(OpCodes.Cgt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.Greater:
                    ilProcessor.Emit(OpCodes.Cgt);
                    break;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    ilProcessor.Emit(OpCodes.Clt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected binary operator {SyntaxFacts.GetText(node.Operator.SyntaxKind)}({node.Left.Type}, {node.Right.Type})");
            }
        }

        private void EmitCallExpression(ILProcessor ilProcessor, BoundCallExpression node)
        {
            foreach (var argument in node.Arguments)
            {
                EmitExpression(ilProcessor, argument);
            }

            if (node.Function == BuiltinFunctions.Input)
            {
                ilProcessor.Emit(OpCodes.Call, _consoleReadLineReference);
            }
            else if (node.Function == BuiltinFunctions.Print)
            {
                ilProcessor.Emit(OpCodes.Call, _consoleWriteLineReference);
            }
            else
            {
                var methodDefinition = _methods[node.Function];
                ilProcessor.Emit(OpCodes.Call, methodDefinition);
            }
        }

        private void EmitConversionExpression(ILProcessor ilProcessor, BoundConversionExpression node)
        {
            EmitExpression(ilProcessor, node.Expression);
            var needsBoxing = node.Expression.Type == TypeSymbol.Bool ||
                              node.Expression.Type == TypeSymbol.Int;
            if (needsBoxing)
            {
                ilProcessor.Emit(OpCodes.Box, Import(node.Expression.Type));
            }

            if (node.Type == TypeSymbol.Any)
            {
                // Done
            }
            else if (node.Type == TypeSymbol.Bool)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToBooleanReference);
            }
            else if (node.Type == TypeSymbol.Int)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToInt32Reference);
            }
            else if (node.Type == TypeSymbol.String)
            {
                ilProcessor.Emit(OpCodes.Call, _convertToStringReference);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected convertion from {node.Expression.Type} to {node.Type}");
            }
        }
    }
}