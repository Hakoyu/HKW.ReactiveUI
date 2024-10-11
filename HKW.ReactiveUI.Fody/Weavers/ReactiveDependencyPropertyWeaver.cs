//// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for full license information.

//using Mono.Cecil;
//using Mono.Cecil.Cil;
//using Mono.Cecil.Rocks;

//namespace HKW.HKWReactiveUI.Fody;

///// <summary>
///// Weaver that generates an ObservableAsPropertyHelper.
///// </summary>
//internal static class ReactiveDependencyPropertyWeaver
//{
//    /// <summary>
//    /// Executes this instance.
//    /// </summary>
//    /// <exception cref="Exception">
//    /// reactiveObjectExtensions is null
//    /// or
//    /// raisePropertyChangedMethod is null
//    /// or
//    /// reactiveDecoratorAttribute is null.
//    /// </exception>
//    public static void Execute(ModuleDefinition moduleDefinition)
//    {
//        ModuleWeaver.Logger.LogInfo(nameof(ReactiveDependencyPropertyWeaver));

//        var reactiveDependencyAttribute =
//            moduleDefinition.FindType(
//                "HKW.HKWReactiveUI",
//                "ReactiveDependencyAttribute",
//                ModuleWeaver.HKWReactiveUI
//            ) ?? throw new Exception("reactiveDecoratorAttribute is null");
//        foreach (
//            var targetType in ModuleWeaver.IReactiveObjectDerivedClasses.Where(x =>
//                x.Properties.Any(y => y.IsDefined(reactiveDependencyAttribute))
//            )
//        )
//        {
//            foreach (
//                var facadeProperty in targetType.Properties.Where(x =>
//                    x.IsDefined(reactiveDependencyAttribute)
//                )
//            )
//            {
//                // If the property already has a body then do not weave to prevent loss of instructions
//                if (
//                    !facadeProperty.GetMethod.Body.Instructions.Any(x =>
//                        x.Operand is FieldReference
//                    ) || facadeProperty.GetMethod.Body.HasVariables
//                )
//                {
//                    ModuleWeaver.Logger.LogError(
//                        $"Property {facadeProperty.Name} is not an auto property and therefore not suitable for ReactiveDependency weaving"
//                    );
//                    continue;
//                }

//                var attribute = facadeProperty.CustomAttributes.First(x =>
//                    x.AttributeType.FullName == reactiveDependencyAttribute.FullName
//                );

//                var targetNamedArgument = attribute.ConstructorArguments.FirstOrDefault();
//                var targetValue = targetNamedArgument.Value?.ToString();
//                if (string.IsNullOrEmpty(targetValue))
//                {
//                    ModuleWeaver.Logger.LogError("No target property defined on the object");
//                    continue;
//                }

//                if (
//                    targetType.Properties.All(x => x.Name != targetValue)
//                    && targetType.Fields.All(x => x.Name != targetValue)
//                )
//                {
//                    ModuleWeaver.Logger.LogError(
//                        $"dependency object property/field name '{targetValue}' not found on target type {targetType.Name}"
//                    );
//                    continue;
//                }

//                var objPropertyTarget = targetType.Properties.FirstOrDefault(x =>
//                    x.Name == targetValue
//                );
//                var objFieldTarget = targetType.Fields.FirstOrDefault(x => x.Name == targetValue);

//                var objDependencyTargetType = objPropertyTarget is not null
//                    ? objPropertyTarget.PropertyType.Resolve()
//                    : objFieldTarget?.FieldType.Resolve();

//                if (objDependencyTargetType is null)
//                {
//                    ModuleWeaver.Logger.LogError("Couldn't result the dependency type");
//                    continue;
//                }

//                // Look for the target property on the member obj
//                var destinationPropertyNamedArgument = attribute.Properties.FirstOrDefault(x =>
//                    x.Name == "TargetProperty"
//                );
//                var destinationPropertyName =
//                    destinationPropertyNamedArgument.Argument.Value?.ToString();

//                // If no target property was specified use this property's name as the target on the decorated object (ala a decorated property)
//                if (string.IsNullOrEmpty(destinationPropertyName))
//                {
//                    destinationPropertyName = facadeProperty.Name;
//                }

//                if (objDependencyTargetType.Properties.All(x => x.Name != destinationPropertyName))
//                {
//                    ModuleWeaver.Logger.LogError(
//                        $"Target property {destinationPropertyName} on dependency of type {objDependencyTargetType.DeclaringType.Name} not found"
//                    );
//                    continue;
//                }

//                var destinationProperty = objDependencyTargetType.Properties.First(x =>
//                    x.Name == destinationPropertyName
//                );

//                // The property on the facade/decorator should have a setter
//                if (facadeProperty.SetMethod is null)
//                {
//                    ModuleWeaver.Logger.LogError(
//                        $"Property {facadeProperty.DeclaringType.FullName}.{facadeProperty.Name} has no setter, therefore it is not possible for the property to change, and thus should not be marked with [ReactiveDecorator]"
//                    );
//                    continue;
//                }

//                // The property on the dependency should have a setter e.g. Dependency.SomeProperty = value;
//                if (destinationProperty.SetMethod is null)
//                {
//                    ModuleWeaver.Logger.LogError(
//                        $"Dependency object's property {destinationProperty.DeclaringType.FullName}.{destinationProperty.Name} has no setter, therefore it is not possible for the property to change, and thus should not be marked with [ReactiveDecorator]"
//                    );
//                    continue;
//                }

//                // Remove old field (the generated backing field for the auto property)
//                var oldField = (FieldReference)
//                    facadeProperty
//                        .GetMethod.Body.Instructions.Single(x => x.Operand is FieldReference)
//                        .Operand;
//                var oldFieldDefinition = oldField.Resolve();
//                targetType.Fields.Remove(oldFieldDefinition);

//                // See if there exists an initializer for the auto-property
//                var constructors = targetType.Methods.Where(x => x.IsConstructor);
//                foreach (var constructor in constructors)
//                {
//                    var fieldAssignment = constructor.Body.Instructions.SingleOrDefault(x =>
//                        Equals(x.Operand, oldFieldDefinition) || Equals(x.Operand, oldField)
//                    );
//                    if (fieldAssignment is not null)
//                    {
//                        // Replace field assignment with a property set (the stack semantics are the same for both,
//                        // so happily we don't have to manipulate the byte code any further.)
//                        var setterCall = constructor
//                            .Body.GetILProcessor()
//                            .Create(
//                                facadeProperty.SetMethod.IsVirtual
//                                    ? OpCodes.Callvirt
//                                    : OpCodes.Call,
//                                facadeProperty.SetMethod
//                            );
//                        constructor.Body.GetILProcessor().Replace(fieldAssignment, setterCall);
//                    }
//                }

//                // Build out the getter which simply returns the value of the generated field
//                facadeProperty.GetMethod.Body = new MethodBody(facadeProperty.GetMethod);
//                facadeProperty.GetMethod.Body.Emit(il =>
//                {
//                    il.Emit(OpCodes.Ldarg_0);
//                    if (objPropertyTarget is not null)
//                    {
//                        il.Emit(
//                            objPropertyTarget.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call,
//                            objPropertyTarget.GetMethod
//                        );
//                    }
//                    else
//                    {
//                        il.Emit(OpCodes.Ldfld, objFieldTarget);
//                    }

//                    il.Emit(
//                        destinationProperty.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call,
//                        destinationProperty.GetMethod
//                    );
//                    il.Emit(OpCodes.Ret);
//                });

//                TypeReference genericTargetType = targetType;
//                if (targetType.HasGenericParameters)
//                {
//                    var genericDeclaration = new GenericInstanceType(targetType);
//                    foreach (var parameter in targetType.GenericParameters)
//                    {
//                        genericDeclaration.GenericArguments.Add(parameter);
//                    }

//                    genericTargetType = genericDeclaration;
//                }

//                var methodReference = ModuleWeaver.RaiseAndSetIfChangedMethod.MakeGenericMethod(
//                    genericTargetType
//                );
//                facadeProperty.SetMethod.Body = new MethodBody(facadeProperty.SetMethod);
//                facadeProperty.SetMethod.Body.Emit(il =>
//                {
//                    il.Emit(OpCodes.Ldarg_0);
//                    if (objPropertyTarget is not null)
//                    {
//                        il.Emit(
//                            objPropertyTarget.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call,
//                            objPropertyTarget.GetMethod
//                        );
//                    }
//                    else
//                    {
//                        il.Emit(OpCodes.Ldfld, objFieldTarget);
//                    }

//                    il.Emit(OpCodes.Ldarg_1);
//                    il.Emit(
//                        destinationProperty.SetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call,
//                        destinationProperty.SetMethod
//                    ); // Set the nested property
//                    il.Emit(OpCodes.Ldarg_0);
//                    il.Emit(OpCodes.Ldstr, facadeProperty.Name); // "PropertyName"
//                    il.Emit(OpCodes.Call, methodReference); // this.RaisePropertyChanged("PropertyName")
//                    il.Emit(OpCodes.Ret);
//                });
//            }
//        }
//    }
//}
