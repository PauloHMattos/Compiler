using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;
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
        private readonly Dictionary<TypeSymbol, TypeDefinition> _enums;
        private readonly Dictionary<TypeSymbol, TypeDefinition> _structs;
        private readonly Dictionary<FunctionSymbol, MethodDefinition> _methods;
        private readonly Dictionary<VariableSymbol, VariableDefinition> _locals;
        private readonly Dictionary<BoundLabel, int> _labels;
        private readonly Dictionary<SourceText, Document> _documents;
        private readonly List<Fixup> _fixups;

        private readonly MethodReference _objectEqualsReference;
        private readonly MethodReference _consoleReadLineReference;
        private readonly MethodReference _consoleWriteLineReference;
        private readonly MethodReference _stringConcatReference;
        private readonly MethodReference _convertToBooleanReference;
        private readonly MethodReference _convertToInt32Reference;
        private readonly MethodReference _convertToStringReference;
        private readonly MethodReference _debuggableAttributeCtorReference;

        private Emitter(string moduleName, IEnumerable<string> references)
        {
            _diagnostics = new DiagnosticBag();
            _assemblies = new List<AssemblyDefinition>();
            _resolvedTypes = new Dictionary<TypeSymbol, TypeReference>();
            _enums = new Dictionary<TypeSymbol, TypeDefinition>();
            _structs = new Dictionary<TypeSymbol, TypeDefinition>();
            _methods = new Dictionary<FunctionSymbol, MethodDefinition>();
            _locals = new Dictionary<VariableSymbol, VariableDefinition>();

            _labels = new Dictionary<BoundLabel, int>();
            _fixups = new List<Fixup>();
            _documents = new Dictionary<SourceText, Document>();

            var assemblyName = new AssemblyNameDefinition(moduleName, new Version("1.0"));
            _assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);

            ReadAssemblies(references);
            ResolveTypes();

            _objectEqualsReference = ResolveMethod<object>(nameof(object.Equals), new[] { typeof(object), typeof(object) });
            _consoleReadLineReference = ResolveMethod(typeof(Console), nameof(Console.ReadLine), Array.Empty<Type>());
            _consoleWriteLineReference = ResolveMethod(typeof(Console), nameof(Console.WriteLine), new[] { typeof(object) });
            _stringConcatReference = ResolveMethod<string>(nameof(string.Concat), new[] { typeof(string), typeof(string) });
            _convertToBooleanReference = ResolveMethod(typeof(Convert), nameof(Convert.ToBoolean), new[] { typeof(object) });
            _convertToInt32Reference = ResolveMethod(typeof(Convert), nameof(Convert.ToInt32), new[] { typeof(object) });
            _convertToStringReference = ResolveMethod(typeof(Convert), nameof(Convert.ToString), new[] { typeof(object) });
            _debuggableAttributeCtorReference = ResolveMethod<DebuggableAttribute>(".ctor", new [] { typeof(bool), typeof(bool) });

            var objectType = Import(TypeSymbol.Any);
            _typeDefinition = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, objectType);
            _assemblyDefinition.MainModule.Types.Add(_typeDefinition);
        }

        private MethodReference ResolveMethod<T>(string methodName, Type[] parameterTypes)
        {
            return ResolveMethod(typeof(T), methodName, parameterTypes);
        }

        private MethodReference ResolveMethod(Type type, string methodName, Type[] parameterTypes)
        {
            string typeName = type.FullName!;
            var foundTypes = _assemblies.SelectMany(a => a.Modules)
                                       .SelectMany(m => m.Types)
                                       .Where(t => t.FullName == typeName)
                                       .ToArray();

            if (foundTypes.Length == 0)
            {
                _diagnostics.ReportRequiredTypeNotFound(null, typeName);
                return null!;
            }
            else if (foundTypes.Length > 1)
            {
                _diagnostics.ReportRequiredTypeAmbiguous(null, typeName, foundTypes);
                return null!;
            }

            var foundType = foundTypes[0]!;
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
            return null!;
        }
        
        private void ReadAssemblies(IEnumerable<string> references)
        {
            foreach (var reference in references)
            {
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
            foreach (var type in TypeSymbol.GetBuiltInTypes().Concat(TypeSymbol.GetBaseTypes()))
            {
                var typeReference = ResolveType(type);
                if (typeReference == null)
                {
                    continue;
                }
                _resolvedTypes.Add(type, typeReference);
            }
        }
        
        private TypeReference ResolveType(TypeSymbol type)
        {
            if (type.NetType == null)
            {
                return null!;
            }

            string metadataName = type.NetType.FullName!;
            var foundTypes = _assemblies.SelectMany(a => a.Modules)
                                        .SelectMany(a => a.Types)
                                        .Where(t => t.FullName == metadataName)
                                        .ToArray();

            if (foundTypes.Length == 1)
            {
                var typeReference = _assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                return typeReference;
            }

            if (foundTypes.Length == 0)
            {
                _diagnostics.ReportRequiredTypeNotFound(type.Name, metadataName);
            }
            else if (foundTypes.Length > 1)
            {
                _diagnostics.ReportRequiredTypeAmbiguous(type.Name, metadataName, foundTypes);
            }
            return null!;
        }

        internal static ImmutableArray<Diagnostic> Emit(
            BoundProgram program,
            string moduleName,
            IEnumerable<string> references,
            string outputPath)
        {
            if (program.Diagnostics.HasErrors())
            {
                return program.Diagnostics;
            }
            var emitter = new Emitter(moduleName, references);
            return emitter.Emit(program, outputPath);
        }

        private ImmutableArray<Diagnostic> Emit(BoundProgram program, string outputPath)
        {
            _diagnostics.AddRange(program.Diagnostics);
            if (_diagnostics.HasErrors())
            {
                return _diagnostics.ToImmutableArray();
            }

            EmitEnumDeclarations(program.Enums);
            EmitStructDeclarations(program.Structs);
            EmitFunctionDeclarations(program.Functions);

            if (program.MainFunction != null)
            {
                _assemblyDefinition.EntryPoint = _methods[program.MainFunction];
            }

            // TODO: We should not emit this attribute unless we produce a debug build
            var debuggableAttribute = new CustomAttribute(_debuggableAttributeCtorReference);
            debuggableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(Import(TypeSymbol.Bool), true));
            debuggableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(Import(TypeSymbol.Bool), true));
            _assemblyDefinition.CustomAttributes.Add(debuggableAttribute);

            // TODO: We should not be computing paths here
            var symbolsPath = Path.ChangeExtension(outputPath, ".pdb");

            // TODO: We should support not emitting symbols
            using (var outputStream = File.Create(outputPath))
            using (var symbolStream = File.Create(symbolsPath))
            {
                var writerParameters = new WriterParameters
                {
                    WriteSymbols = true,
                    SymbolStream = symbolStream,
                    SymbolWriterProvider = new PortablePdbWriterProvider()
                };
                _assemblyDefinition.Write(outputStream, writerParameters);
            }
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

            // TODO: Only emit this when emitting symbols
            method.DebugInformation.Scope = new ScopeDebugInformation(method.Body.Instructions.First(), method.Body.Instructions.Last());

            foreach (var local in _locals)
            {
                var symbol = local.Key;
                var definition = local.Value;
                var debugInfo = new VariableDebugInformation(definition, symbol.Name);
                method.DebugInformation.Scope.Variables.Add(debugInfo);
            }
        }

        private void EmitEnumDeclarations(ImmutableArray<EnumSymbol> enums)
        {
            foreach (var enumSymbol in enums)
            {
                EmitEnumDeclaration(enumSymbol);
            }
        }

        private void EmitEnumDeclaration(EnumSymbol enumSymbol)
        {
            const TypeAttributes _enumAttributes = TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.AnsiClass | TypeAttributes.Sealed;
            const FieldAttributes _enumFieldAttributes = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault;
            const FieldAttributes _enumSpecialAttributes = FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName;
            
            var enumType = new TypeDefinition("", enumSymbol.Name, _enumAttributes, Import(TypeSymbol.Enum));
            _assemblyDefinition.MainModule.Types.Add(enumType);
            _enums.Add(enumSymbol, enumType);
            _resolvedTypes.Add(enumSymbol, enumType);

            var specialField = new FieldDefinition("value__", _enumSpecialAttributes, Import(TypeSymbol.Int));
            enumType.Fields.Add(specialField);
            foreach (var member in enumSymbol.Members)
            {
                if (member is not EnumValueSymbol value)
                {
                    continue;
                }

                var valueField = new FieldDefinition(value.Name, _enumFieldAttributes, enumType)
                {
                    Constant = value.Constant.Value
                };
                enumType.Fields.Add(valueField);
            }
        }
        
        private void EmitStructDeclarations(ImmutableDictionary<StructSymbol, BoundBlockStatement> structs)
        {
            foreach (var (declaration, _) in structs)
            {
                EmitStructDeclaration(declaration);
            }
            foreach (var (declaration, body) in structs)
            {
                EmitStructBody(declaration, body);
            }
        }
        
        private void EmitStructDeclaration(StructSymbol structSymbol)
        {
            const TypeAttributes _structAttributes = TypeAttributes.Class | TypeAttributes.Public |
                                                    TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass |
                                                    TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
            
            var structType = new TypeDefinition("", structSymbol.Name, _structAttributes, Import(TypeSymbol.Struct));
            _assemblyDefinition.MainModule.Types.Add(structType);
            _structs.Add(structSymbol, structType);
            _resolvedTypes.Add(structSymbol, structType);

            // Forward-declare empty constructor
            var emptyCtorDefinition = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName |
                MethodAttributes.HideBySig,
                Import(TypeSymbol.Void)
            );
            structType.Methods.Insert(0, emptyCtorDefinition);

            // Forward-declare initializer constructor
            var defaultCtorDefintion = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName |
                MethodAttributes.HideBySig,
                Import(TypeSymbol.Void)
            );

            // This constructor will be the second one on the class
            structType.Methods.Insert(0, defaultCtorDefintion);
        }

        private void EmitStructBody(StructSymbol key, BoundBlockStatement value)
        {
            var structType = _structs[key];
            EmitEmptyConstructorForStruct(value, structType);
            EmitDefaultConstructorForStruct(key, structType);
        }

        private void EmitEmptyConstructorForStruct(BoundBlockStatement value, TypeDefinition structType)
        {
            // Get empty constructor declaration
            var constructor = structType.Methods[0];

            var ilProcessor = constructor.Body.GetILProcessor();

            foreach (var field in value.Statements)
            {
                if (field is BoundVariableDeclarationStatement d)
                {
                    var fieldAttributes = d.Variable.IsReadOnly ? FieldAttributes.Public | FieldAttributes.InitOnly : FieldAttributes.Public;
                    var fieldDefinition = new FieldDefinition(d.Variable.Name, fieldAttributes, Import(d.Variable.Type));
                    structType.Fields.Add(fieldDefinition);

                    EmitFieldAssignment(ilProcessor, d, fieldDefinition);
                }
                else if (field is BoundSequencePointStatement s && s.Statement is BoundVariableDeclarationStatement sd)
                {
                    var fieldAttributes = sd.Variable.IsReadOnly ? FieldAttributes.Public | FieldAttributes.InitOnly : FieldAttributes.Public;
                    var fieldDefinition = new FieldDefinition(sd.Variable.Name, fieldAttributes, Import(sd.Variable.Type));
                    structType.Fields.Add(fieldDefinition);

                    //EmitSequencePointStatement(ilProcessor, s);
                    EmitFieldAssignment(ilProcessor, sd, fieldDefinition);
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected statement type {field.Kind}. Expected BoundVariableDeclaration.");
                }
            }

            ilProcessor.Emit(OpCodes.Ret);
            constructor.Body.Optimize();
        }

        private void EmitDefaultConstructorForStruct(StructSymbol structSymbol, TypeDefinition structType)
        {
            // Get default constructor declaration
            var constructor = structType.Methods[1];

            var ilProcessor = constructor.Body.GetILProcessor();

            // Assign each parameter
            for (int i = 0; i < structSymbol.CtorParameters.Length; i++)
            {
                var ctorParam = structSymbol.CtorParameters[i];
                var paramType = Import(ctorParam.Type);
                const ParameterAttributes parameterAttributes = ParameterAttributes.None;
                var parameterDefinition = new ParameterDefinition(ctorParam.Name, parameterAttributes, paramType);

                constructor.Parameters.Add(parameterDefinition);

                ilProcessor.Emit(OpCodes.Ldarg_0);
                ilProcessor.Emit(OpCodes.Ldarg, i + 1);

                foreach (var field in structType.Fields)
                {
                    if (field.Name == ctorParam.Name)
                    {
                        ilProcessor.Emit(OpCodes.Stfld, field);
                        break;
                    }
                }
            }

            ilProcessor.Emit(OpCodes.Ret);
            constructor.Body.Optimize();
        }

        private TypeReference Import(TypeSymbol type)
        {
            return _resolvedTypes[type];
        }

        private void EmitStatement(ILProcessor ilProcessor, BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.NopStatement:
                    EmitNopStatement(ilProcessor, (BoundNopStatement)node);
                    break;
                case BoundNodeKind.SequencePointStatement:
                    EmitSequencePointStatement(ilProcessor, (BoundSequencePointStatement)node);
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

        private void EmitSequencePointStatement(ILProcessor ilProcessor, BoundSequencePointStatement node)
        {
            var index = ilProcessor.Body.Instructions.Count;
            EmitStatement(ilProcessor, node.Statement);
            var instruction = ilProcessor.Body.Instructions[index];

            var document = GetDocument(node.Location.Text);
            var sequencePoint = new SequencePoint(instruction, document)
            {
                StartLine = node.Location.StartLine + 1,
                EndLine = node.Location.EndLine + 1,
                StartColumn = node.Location.StartCharacter + 1,
                EndColumn = node.Location.EndCharacter + 1
            };

            ilProcessor.Body.Method.DebugInformation.SequencePoints.Add(sequencePoint);
        }

        private Document GetDocument(SourceText sourceText)
        {
            if (!_documents.TryGetValue(sourceText, out var document))
            {
                var fullPath = Path.GetFullPath(sourceText.FileName);
                document = new Document(fullPath);
                _documents.Add(sourceText, document);
            }
            return document;
        }

        private void EmitNopStatement(ILProcessor ilProcessor, BoundNopStatement node)
        {
            ilProcessor.Emit(OpCodes.Nop);
        }

        private void EmitFieldAssignment(ILProcessor ilProcessor, BoundVariableDeclarationStatement node, FieldDefinition field)
        {
            ilProcessor.Emit(OpCodes.Ldarg_0);
            EmitExpression(ilProcessor, node.Initializer);
            ilProcessor.Emit(OpCodes.Stfld, field);
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
                // TODO - Take a better look a this
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
                case BoundNodeKind.TypeReferenceExpression:
                    EmitTypeReferenceExpression(ilProcessor, (BoundTypeReferenceExpression)node);
                    break;
                case BoundNodeKind.MemberAccessExpression:
                    EmitMemberAccessExpression(ilProcessor, (BoundMemberAccessExpression)node);
                    break;
                case BoundNodeKind.MemberAssignmentExpression:
                    EmitMemberAssignmentExpression(ilProcessor, (BoundMemberAssignmentExpression)node);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitTypeReferenceExpression(ILProcessor ilProcessor, BoundTypeReferenceExpression node)
        {
            // HACK - This is not the rigth way to handle statics
            // Currently I don't know how to properly do it, so...
        }

        private void EmitConstantExpression(ILProcessor ilProcessor, BoundExpression node)
        {
            Debug.Assert(node.ConstantValue != null);

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
                if (node.ByReference)
                {
                    ilProcessor.Emit(OpCodes.Ldarga, parameter.Ordinal);
                }
                else
                {
                    ilProcessor.Emit(OpCodes.Ldarg, parameter.Ordinal);
                }
            }
            else
            {
                var variableDefinition = _locals[node.Variable];
                
                if (node.ByReference)
                {
                    ilProcessor.Emit(OpCodes.Ldloca_S, variableDefinition);
                }
                else
                {
                    ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
                }
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
            else if (node.Function.Name.EndsWith(".ctor"))
            {
                var className = node.Function.Name[..^5];
                var structSymbol = _structs.First(s => s.Key.Name == className).Value;
                
                // TODO: We should use a general overload resolution algorithm instead
                ilProcessor.Emit(OpCodes.Newobj, node.Arguments.Length == 0 ?
                                                    structSymbol.Methods[0] :
                                                    structSymbol.Methods[1]);
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
                              node.Expression.Type == TypeSymbol.Int ||
                              node.Expression.Type.IsEnum();

            var expressionType = Import(node.Expression.Type);
            if (needsBoxing)
            {
                ilProcessor.Emit(OpCodes.Box, expressionType);
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
        
        private void EmitMemberAccessExpression(ILProcessor ilProcessor, BoundMemberAccessExpression node)
        {
            var typeDefinition = Import(node.Instance.Type).Resolve();
            Debug.Assert(typeDefinition != null);

            EmitExpression(ilProcessor, node.Instance);
            
            switch (node.Member.MemberKind)
            {
                case MemberKind.Field:
                    EmitFieldAccessExpression(ilProcessor, node, typeDefinition);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected member type {node.Member.Kind}");
            }
        }

        private static void EmitFieldAccessExpression(ILProcessor ilProcessor, BoundMemberAccessExpression node, TypeDefinition typeDefinition)
        {
            foreach (var field in typeDefinition.Fields)
            {
                if (field.Name == node.Member.Name)
                {
                    if (field.Constant != null)
                    {
                        ilProcessor.Emit(OpCodes.Ldc_I4, (int)field.Constant);
                    }
                    else
                    {
                        ilProcessor.Emit(OpCodes.Ldfld, field);
                    }
                    break;
                }
            }
        }

        private void EmitMemberAssignmentExpression(ILProcessor ilProcessor, BoundMemberAssignmentExpression node)
        {
            Debug.Assert(node.Instance.Type != null);

            EmitExpression(ilProcessor, node.Instance);
            EmitExpression(ilProcessor, node.Expression);

            var typeReference = Import(node.Instance.Type);
            var typeDefinition = typeReference.Resolve();
            
            ilProcessor.Emit(OpCodes.Dup);
            var expressionTypeReference = Import(node.Expression.Type);
            var variableDefinition = new VariableDefinition(expressionTypeReference);
            ilProcessor.Body.Variables.Add(variableDefinition);
            ilProcessor.Emit(OpCodes.Stloc, variableDefinition);

            switch (node.Member.MemberKind)
            {
                case MemberKind.Field:
                    EmitFieldAssignmentExpression(ilProcessor, node, typeDefinition);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected member type {node.Member.Kind}");
            }

            ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
        }

        private static void EmitFieldAssignmentExpression(ILProcessor ilProcessor, BoundMemberAssignmentExpression node, TypeDefinition typeDefinition)
        {
            foreach (var field in typeDefinition.Fields)
            {
                if (field.Name == node.Member.Name)
                {
                    ilProcessor.Emit(OpCodes.Stfld, field);
                    break;
                }
            }
        }
    }
}