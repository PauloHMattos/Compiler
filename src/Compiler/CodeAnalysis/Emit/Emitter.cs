using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Compiler.CodeAnalysis.Binding;
using Compiler.CodeAnalysis.Binding.Scopes;
using Compiler.CodeAnalysis.Diagnostics;
using Compiler.CodeAnalysis.Symbols;
using Compiler.CodeAnalysis.Syntax;
using Compiler.CodeAnalysis.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace Compiler.CodeAnalysis.Emit
{
    internal class Emitter
    {
        const TypeAttributes _enumAttributes = TypeAttributes.Class
                                               | TypeAttributes.NotPublic
                                               | TypeAttributes.AnsiClass
                                               | TypeAttributes.Sealed;

        const TypeAttributes _structAttributes = TypeAttributes.Class
                                                 //| TypeAttributes.Public
                                                 | TypeAttributes.SequentialLayout
                                                 | TypeAttributes.AnsiClass
                                                 | TypeAttributes.Sealed
                                                 | TypeAttributes.BeforeFieldInit;
        private readonly DiagnosticBag _diagnostics;
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly TypeDefinition _typeDefinition;
        private readonly List<AssemblyDefinition> _assemblies;
        private readonly Dictionary<TypeSymbol, TypeReference> _resolvedTypes;
        private readonly Dictionary<TypeSymbol, TypeDefinition> _declaredTypes;
        private readonly Dictionary<FunctionSymbol, MethodDefinition> _methods;
        private readonly Dictionary<VariableSymbol, VariableDefinition> _locals;
        private readonly Dictionary<BoundLabel, int> _labels;
        private readonly Dictionary<SourceText, Document> _documents;
        private readonly List<(int InstructionIndex, BoundLabel Target)> _fixups;

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
            _declaredTypes = new Dictionary<TypeSymbol, TypeDefinition>();
            _methods = new Dictionary<FunctionSymbol, MethodDefinition>();
            _locals = new Dictionary<VariableSymbol, VariableDefinition>();

            _labels = new Dictionary<BoundLabel, int>();
            _fixups = new List<(int InstructionIndex, BoundLabel Target)>();
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
            _debuggableAttributeCtorReference = ResolveMethod<DebuggableAttribute>(".ctor", new[] { typeof(bool), typeof(bool) });

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

            EmitTypeDeclarations(program.Types);
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

        private void EmitTypeDeclarations(ImmutableArray<TypeSymbol> types)
        {
            foreach (var typeSymbol in types)
            {
                Debug.Assert(typeSymbol.Declaration != null);
                EmitTypeDeclaration(typeSymbol);
            }

            foreach (var typeSymbol in types)
            {
                Debug.Assert(typeSymbol.Declaration != null);
                EmitTypeBody(typeSymbol);
            }
        }

        private void EmitTypeBody(TypeSymbol typeSymbol)
        {
            Debug.Assert(typeSymbol.BoundScope != null);

            var typeDefinition = _declaredTypes[typeSymbol];
            EmitFields(typeSymbol, typeDefinition);

            switch (typeSymbol.Declaration!.TypeKind)
            {
                case TypeDeclarationKind.Enum:
                    break;

                case TypeDeclarationKind.Struct:
                    EmitStructBody((StructSymbol)typeSymbol, typeDefinition);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected declaration kind {typeSymbol.Declaration.TypeKind}");
            }

            
            var nestedTypes = typeSymbol.BoundScope.GetDeclaredSymbols<TypeSymbol>();
            foreach (var nestedType in nestedTypes)
            {
                EmitTypeBody(nestedType);
            }
        }

        private void EmitFields(TypeSymbol typeSymbol, TypeDefinition typeDefinition)
        {
            foreach (var field in typeSymbol.Members.OfType<FieldSymbol>())
            {
                var fieldAttributes = FieldAttributes.Public;

                if (field.IsStatic)
                {
                    fieldAttributes |= FieldAttributes.Static;
                    if (field.IsReadOnly)
                    {
                        fieldAttributes |= FieldAttributes.Literal;
                    }
                }
                else if (field.IsReadOnly)
                {
                    fieldAttributes |= FieldAttributes.InitOnly;
                }

                var fieldDefinition = new FieldDefinition(field.Name, fieldAttributes, Import(field.Type));
                if (field.Constant != null)
                {
                    fieldDefinition.Constant = field.Constant.Value;
                    fieldDefinition.Attributes |= FieldAttributes.HasDefault;
                }
                typeDefinition.Fields.Add(fieldDefinition);
            }
        }

        private void EmitTypeDeclaration(TypeSymbol typeSymbol)
        {
            Debug.Assert(typeSymbol.BoundScope != null);

            TypeAttributes? modifiers = null;
            if (typeSymbol.BoundScope.Parent is TypeBoundScope)
            {
                modifiers = TypeAttributes.NestedPublic;
            }

            switch (typeSymbol.Declaration!.TypeKind)
            {
                case TypeDeclarationKind.Enum:
                    EmitEnumDeclaration((EnumSymbol)typeSymbol, modifiers);
                    break;

                case TypeDeclarationKind.Struct:
                    EmitStructDeclaration((StructSymbol)typeSymbol, modifiers);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected declaration kind {typeSymbol.Declaration.TypeKind}");
            }

            var nestedTypes = typeSymbol.BoundScope.GetDeclaredSymbols<TypeSymbol>();
            foreach (var nestedType in nestedTypes)
            {
                EmitTypeDeclaration(nestedType);
            }
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
            var functionType = Import(function.ReturnType);
            var methodAttributes = function.ReceiverType == null ? MethodAttributes.Static
                                                               | MethodAttributes.Public
                                                             : MethodAttributes.Public;
            var method = new MethodDefinition(function.Name, methodAttributes, functionType);

            foreach (var parameter in function.Parameters)
            {
                var parameterType = Import(parameter.Type);
                var parameterAttributes = ParameterAttributes.None;
                var parameterDefinition = new ParameterDefinition(parameter.Name, parameterAttributes, parameterType);
                method.Parameters.Add(parameterDefinition);
            }

            if (function.ReceiverType == null)
            {
                _typeDefinition.Methods.Add(method);
            }
            else
            {
                _declaredTypes[function.ReceiverType].Methods.Add(method);
            }

            _methods.Add(function, method);
        }

        private void EmitFunctionBody(FunctionSymbol function, BoundBlockStatement body)
        {
            Debug.Assert(function.Declaration != null);

            var method = _methods[function];
            _locals.Clear();
            _labels.Clear();
            _fixups.Clear();
            
            var ilProcessor = method.Body.GetILProcessor();
            var index = ilProcessor.Body.Instructions.Count;
            EmitNopStatement(ilProcessor);
            EmitSequencePoint(ilProcessor, index, function.Declaration.OpenParenthesisToken.Location);

            foreach (var statement in body.Statements)
            {
                EmitStatement(ilProcessor, statement);
            }

            foreach (var (instructionIndex, targetLabel) in _fixups)
            {
                var targetInstructionIndex = _labels[targetLabel];
                var targetInstruction = ilProcessor.Body.Instructions[targetInstructionIndex];
                var instructionToFixup = ilProcessor.Body.Instructions[instructionIndex];
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

        private void EmitEnumDeclaration(EnumSymbol enumSymbol, TypeAttributes? modifiers)
        {
            const FieldAttributes _enumSpecialAttributes = FieldAttributes.Public
                                                           | FieldAttributes.SpecialName
                                                           | FieldAttributes.RTSpecialName;

            var attributes = _enumAttributes;
            if (modifiers.HasValue)
            {
                attributes |= modifiers.Value;
            }

            var enumType = new TypeDefinition("", enumSymbol.Name, attributes, Import(TypeSymbol.Enum));
            GetCollectionFor(enumSymbol).Add(enumType);
            _declaredTypes.Add(enumSymbol, enumType);
            _resolvedTypes.Add(enumSymbol, enumType);

            var specialField = new FieldDefinition("value__", _enumSpecialAttributes, Import(TypeSymbol.Int));
            enumType.Fields.Add(specialField);
        }
        
        private Collection<TypeDefinition> GetCollectionFor(TypeSymbol type)
        {
            Debug.Assert(type.BoundScope != null);
            if (type.BoundScope.Parent is not TypeBoundScope parentScope)
            {
                return _assemblyDefinition.MainModule.Types;
            }
            if (type.BoundScope.Parent == null)
            {
                return _assemblyDefinition.MainModule.Types;
            }
            var parentType = parentScope.OwnerType;
            var parentTypeDefinition = _declaredTypes[parentType];
            return parentTypeDefinition.NestedTypes;
        }

        private void EmitStructDeclaration(StructSymbol structSymbol, TypeAttributes? modifiers)
        {
            var attributes = _structAttributes;
            if (modifiers.HasValue)
            {
                attributes |= modifiers.Value;
            }
            var structType = new TypeDefinition("", structSymbol.Name, attributes, Import(TypeSymbol.Struct));
            GetCollectionFor(structSymbol).Add(structType);
            _declaredTypes.Add(structSymbol, structType);
            _resolvedTypes.Add(structSymbol, structType);
            
            // Forward-declare empty constructor
            var emptyCtorDefinition = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName
                | MethodAttributes.HideBySig,
                Import(TypeSymbol.Void)
            );
            structType.Methods.Add(emptyCtorDefinition);
        }

        private void EmitStructBody(StructSymbol structSymbol, TypeDefinition typeDefinition)
        {
            EmitEmptyConstructorForStruct(structSymbol, typeDefinition);
        }

        private void EmitEmptyConstructorForStruct(StructSymbol structSymbol, TypeDefinition typeDefinition)
        {
            // Get empty constructor declaration
            var constructor = typeDefinition.Methods[0];
            var ilProcessor = constructor.Body.GetILProcessor();

            foreach (var field in structSymbol.Members.OfType<FieldSymbol>())
            {
                var fieldDefinition = typeDefinition.Fields.Single(f => f.Name == field.Name);
                EmitFieldAssignment(ilProcessor, field.Initializer, fieldDefinition);
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
                    EmitNopStatement(ilProcessor);
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
            EmitSequencePoint(ilProcessor, index, node.Location);
        }

        private void EmitSequencePoint(ILProcessor ilProcessor, int index, TextLocation location)
        {
            var instruction = ilProcessor.Body.Instructions[index];
            var document = GetDocument(location.Text);
            var sequencePoint = new SequencePoint(instruction, document)
            {
                StartLine = location.StartLine + 1,
                EndLine = location.EndLine + 1,
                StartColumn = location.StartCharacter + 1,
                EndColumn = location.EndCharacter + 1
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

        private static void EmitNopStatement(ILProcessor ilProcessor)
        {
            ilProcessor.Emit(OpCodes.Nop);
        }

        private void EmitFieldAssignment(ILProcessor ilProcessor, BoundExpression expression, FieldDefinition field)
        {
            ilProcessor.Emit(OpCodes.Ldarg_0);
            EmitExpression(ilProcessor, expression);
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
            _fixups.Add((ilProcessor.Body.Instructions.Count, node.Label));
            ilProcessor.Emit(OpCodes.Br, Instruction.Create(OpCodes.Nop));
        }

        private void EmitConditionalGotoStatement(ILProcessor ilProcessor, BoundConditionalGotoStatement node)
        {
            EmitExpression(ilProcessor, node.Condition);

            var opCode = node.JumpIfTrue ? OpCodes.Brtrue : OpCodes.Brfalse;
            _fixups.Add((ilProcessor.Body.Instructions.Count, node.Label));
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

                case BoundNodeKind.SelfExpression:
                    EmitSelfExpression(ilProcessor);
                    break;

                case BoundNodeKind.MemberExpression:
                    EmitMemberExpression(ilProcessor, (BoundMemberExpression)node);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitMemberExpression(ILProcessor ilProcessor, BoundMemberExpression node)
        {
            Console.WriteLine($"EmitMemberExpression: {node}");
            Debug.Assert(node.Symbol.ReceiverType != null);
            var typeDefinition = Import(node.Symbol.ReceiverType).Resolve();
            Debug.Assert(typeDefinition != null);

            switch (node.MemberKind)
            {
                case MemberKind.Field:
                    EmitFieldAccessExpression(ilProcessor, (FieldSymbol)node.Symbol, typeDefinition);
                    break;
                    
                case MemberKind.Method:
                    EmitCallExpression(ilProcessor, (BoundCallExpression)node, typeDefinition);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected member type {node.MemberKind}");
            }
        }

        private static void EmitTypeReferenceExpression(ILProcessor ilProcessor, BoundTypeReferenceExpression node)
        {
            // HACK - This is not the rigth way to handle statics
            // Currently I don't know how to properly do it, so...
        }

        private static void EmitConstantExpression(ILProcessor ilProcessor, BoundExpression node)
        {
            EmitConstantExpression(ilProcessor, node.Type, node.ConstantValue);
        }

        private static void EmitConstantExpression(ILProcessor ilProcessor, TypeSymbol type, BoundConstant? constant)
        {
            Debug.Assert(constant != null);
            if (type == TypeSymbol.Bool)
            {
                var value = (bool)constant.Value;
                var instruction = value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
                ilProcessor.Emit(instruction);
            }
            else if (type == TypeSymbol.Int)
            {
                var value = (int)constant.Value;
                ilProcessor.Emit(OpCodes.Ldc_I4, value);
            }
            else if (type == TypeSymbol.String)
            {
                var value = (string)constant.Value;
                ilProcessor.Emit(OpCodes.Ldstr, value);
            }
            /*
            else if (type.IsEnum())
            {
                var value = (int)constant.Value;
                ilProcessor.Emit(OpCodes.Ldc_I4, value);
            }
            */
            else
            {
                throw new InvalidOperationException($"Unexpected constant expression type: {type}");
            }
        }

        private void EmitVariableExpression(ILProcessor ilProcessor, BoundVariableExpression node)
        {
            var byReference = node.ByReference;
            switch (node.Variable.VariableKind)
            {
                case VariableKind.Parameter:
                    EmitParameterSymbol(ilProcessor, node.Variable, byReference);
                    break;

                case VariableKind.Local:
                    EmitLocalSymbol(ilProcessor, node.Variable, byReference);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid kind {node.Variable.Kind}");
            }
        }

        private void EmitLocalSymbol(ILProcessor ilProcessor, VariableSymbol local, bool byReference)
        {
            var variableDefinition = _locals[local];
            if (byReference)
            {
                ilProcessor.Emit(OpCodes.Ldloca_S, variableDefinition);
            }
            else
            {
                ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
            }
        }

        private static void EmitParameterSymbol(ILProcessor ilProcessor, VariableSymbol parameter, bool byReference)
        {
            Debug.Assert(parameter.VariableKind == VariableKind.Parameter);

            var ordinal = GetParameterOrdinal(ilProcessor.Body.Method, parameter);
            if (!ordinal.HasValue)
            {
                throw new InvalidOperationException($"Parameter with name {parameter.Name} not found");
            }

            if (byReference)
            {
                ilProcessor.Emit(OpCodes.Ldarga, ordinal.Value);
            }
            else
            {
                ilProcessor.Emit(OpCodes.Ldarg, ordinal.Value);
            }
        }
       

        private void EmitAssignmentExpression(ILProcessor ilProcessor, BoundAssignmentExpression node)
        {
            EmitAssignmentPreFix(ilProcessor, node);
            EmitExpression(ilProcessor, node.Right);
            EmitAssignmentPostFix(ilProcessor, node);
        }

        private void EmitAssignmentPreFix(ILProcessor ilProcessor, BoundAssignmentExpression node)
        {
            var expression = node.Left;
            switch (expression.Kind)
            {
                case BoundNodeKind.VariableExpression:
                    break;

                case BoundNodeKind.MemberAccessExpression:
                    var memberExpression = (BoundMemberAccessExpression)expression;
                    EmitExpression(ilProcessor, memberExpression.Instance);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        private void EmitAssignmentPostFix(ILProcessor ilProcessor, BoundAssignmentExpression node)
        {
            var expression = node.Left;
            switch (expression.Kind)
            {
                case BoundNodeKind.VariableExpression:
                    var variableExpression = (BoundVariableExpression)expression;
                    var variableDefinition = _locals[variableExpression.Variable];
                    ilProcessor.Emit(OpCodes.Dup);
                    ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
                    break;

                case BoundNodeKind.MemberAccessExpression:
                    var memberExpression = (BoundMemberAccessExpression)expression;
                    var field = (FieldSymbol)memberExpression.Member.Symbol;
                    var typeDefinition = Import(memberExpression.Instance.Type).Resolve();
                    var fieldDefinition = GetField(field, typeDefinition);

                    ilProcessor.Emit(OpCodes.Dup);
                    var expressionTypeReference = Import(memberExpression.Type);
                    var tempVariable = new VariableDefinition(expressionTypeReference);
                    ilProcessor.Body.Variables.Add(tempVariable);
                    ilProcessor.Emit(OpCodes.Stloc, tempVariable);

                    ilProcessor.Emit(OpCodes.Stfld, fieldDefinition);
                    ilProcessor.Emit(OpCodes.Ldloc, tempVariable);
                    break;

                default:
                    throw new InvalidOperationException();
            }
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

        private void EmitCallExpression(ILProcessor ilProcessor, BoundCallExpression node, TypeDefinition? typeDefinition = null)
        {
            var function = (FunctionSymbol)node.Symbol;
            EmitExpressions(ilProcessor, node.Arguments);

            if (function == BuiltinFunctions.Input)
            {
                ilProcessor.Emit(OpCodes.Call, _consoleReadLineReference);
            }
            else if (function == BuiltinFunctions.Print)
            {
                ilProcessor.Emit(OpCodes.Call, _consoleWriteLineReference);
            }
            else if (node.Symbol.Name.Equals(".ctor"))
            {
                if (typeDefinition == null)
                {
                    typeDefinition = Import(node.Symbol.Type).Resolve();
                }
                Debug.Assert(typeDefinition != null);
                // TODO: We should use a general overload resolution algorithm instead
                ilProcessor.Emit(OpCodes.Newobj, typeDefinition.Methods[0]);
            }
            else
            {
                var methodDefinition = _methods[function];
                ilProcessor.Emit(OpCodes.Call, methodDefinition);
            }
        }

        private void EmitExpressions(ILProcessor ilProcessor, ImmutableArray<BoundExpression> arguments)
        {
            foreach (var argument in arguments)
            {
                if (argument.Kind == BoundNodeKind.SelfExpression)
                {
                    EmitSelfExpression(ilProcessor);
                    var type = Import(argument.Type);
                    if (type.IsValueType)
                    {
                        ilProcessor.Emit(OpCodes.Ldobj, type);
                    }
                    continue;
                }
                EmitExpression(ilProcessor, argument);
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
            if (node.Type.IsEnum())
            {
                var field = (FieldSymbol)node.Member.Symbol;
                EmitConstantExpression(ilProcessor, TypeSymbol.Int, field.Constant);
                return;
            }

            Console.WriteLine("EmitMemberAccessExpression");
            EmitExpression(ilProcessor, node.Instance);
            EmitMemberExpression(ilProcessor, node.Member);
        }

        private static void EmitFieldAccessExpression(ILProcessor ilProcessor, FieldSymbol field, TypeDefinition typeDefinition)
        {
            if (field.Constant != null)
            {
                EmitConstantExpression(ilProcessor, field.Type, field.Constant);
            }

            var fieldDefinition = GetField(field, typeDefinition);
            Debug.Assert(fieldDefinition != null);
            ilProcessor.Emit(OpCodes.Ldfld, fieldDefinition);
        }

        private static FieldDefinition? GetField(MemberSymbol member, TypeDefinition typeDefinition)
        {
            return GetField(member.Name, typeDefinition);
        }

        private static FieldDefinition? GetField(string fieldName, TypeDefinition typeDefinition)
        {
            foreach (var field in typeDefinition.Fields)
            {
                if (field.Name == fieldName)
                {
                    return field;
                }
            }
            return null;
        }

        private static int? GetParameterOrdinal(MethodDefinition method, VariableSymbol parameter)
        {
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var other = method.Parameters[i];
                if (other.Name == parameter.Name)
                {
                    return i + (method.HasThis ? 1 : 0);
                }
            }
            return null;
        }

        private static void EmitSelfExpression(ILProcessor ilProcessor)
        {
            ilProcessor.Emit(OpCodes.Ldarg, ilProcessor.Body.ThisParameter);
        }
    }
}