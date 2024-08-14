// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace HKW.HKWReactiveUI.Fody;

/// <summary>
/// Weaver that replaces properties marked with `[DataMember]` on subclasses of `ReactiveObject` with an
/// implementation that invokes `RaisePropertyChanged` as is required for ReactiveUI.
/// </summary>
internal static class ReactivePropertyWeaver
{
    /// <summary>
    /// Executes this property weaver.
    /// </summary>
    /// <exception cref="Exception">
    /// reactiveObjectExtensions is null
    /// or
    /// raiseAndSetIfChangedMethod is null
    /// or
    /// reactiveAttribute is null
    /// or
    /// [ReactiveProperty] is decorating " + property.DeclaringType.FullName + "." + property.Name + ", but the property has no setter so there would be nothing to react to.  Consider removing the attribute.
    /// </exception>
    public static void Execute(ModuleDefinition moduleDefinition)
    {
        ModuleWeaver.Logger.LogInfo(nameof(ReactivePropertyWeaver));

        var reactivePropertyAttribute =
            moduleDefinition.FindType(
                "HKW.HKWReactiveUI",
                "ReactivePropertyAttribute",
                ModuleWeaver.HKWReactiveUI
            ) ?? throw new Exception("ReactivePropertyAttribute is null");
        foreach (var targetType in ModuleWeaver.IReactiveObjectDerivedClasses)
        {
            foreach (
                var property in targetType.Properties.Where(x =>
                    x.IsDefined(reactivePropertyAttribute)
                )
            )
            {
                if (property.SetMethod is null)
                {
                    ModuleWeaver.Logger.LogError(
                        $"Property {property.DeclaringType.FullName}.{property.Name} has no setter, therefore it is not possible for the property to change, and thus should not be marked with [ReactiveProperty]"
                    );
                    continue;
                }
                var skipCheck =
                    property
                        .CustomAttributes.First(x =>
                            x.AttributeType.FullName == reactivePropertyAttribute.FullName
                        )
                        ?.ConstructorArguments[0]
                        .Value
                        is true;

                // Declare a field to store the property value
                var field = new FieldDefinition(
                    "$" + property.Name,
                    FieldAttributes.Private,
                    property.PropertyType
                );
                targetType.Fields.Add(field);

                // Remove old field (the generated backing field for the auto property)
                var oldField = (FieldReference)
                    property
                        .GetMethod.Body.Instructions.Single(x => x.Operand is FieldReference)
                        .Operand;
                var oldFieldDefinition = oldField.Resolve();
                targetType.Fields.Remove(oldFieldDefinition);

                // See if there exists an initializer for the auto-property
                var constructors = targetType.Methods.Where(x => x.IsConstructor);
                foreach (var constructor in constructors)
                {
                    var fieldAssignment = constructor.Body.Instructions.SingleOrDefault(x =>
                        Equals(x.Operand, oldFieldDefinition) || Equals(x.Operand, oldField)
                    );
                    if (fieldAssignment is not null)
                    {
                        // Replace field assignment with a property set (the stack semantics are the same for both,
                        // so happily we don't have to manipulate the bytecode any further.)
                        var setterCall = constructor
                            .Body.GetILProcessor()
                            .Create(
                                property.SetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call,
                                property.SetMethod
                            );
                        constructor.Body.GetILProcessor().Replace(fieldAssignment, setterCall);
                    }
                }

                // Build out the getter which simply returns the value of the generated field
                property.GetMethod.Body = new MethodBody(property.GetMethod);
                property.GetMethod.Body.Emit(il =>
                {
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldfld, field.BindDefinition(targetType)); // pop -> this.$PropertyName
                    il.Emit(OpCodes.Ret); // Return the field value that is lying on the stack
                });

                TypeReference genericTargetType = targetType;
                if (targetType.HasGenericParameters)
                {
                    var genericDeclaration = new GenericInstanceType(targetType);
                    foreach (var parameter in targetType.GenericParameters)
                    {
                        genericDeclaration.GenericArguments.Add(parameter);
                    }

                    genericTargetType = genericDeclaration;
                }

                var methodReference = skipCheck
                    ? ModuleWeaver.RaiseAndSetMethod.MakeGenericMethod(
                        genericTargetType,
                        property.PropertyType
                    )
                    : ModuleWeaver.RaiseAndSetIfChangedMethod.MakeGenericMethod(
                        genericTargetType,
                        property.PropertyType
                    );

                // Build out the setter which fires the RaiseAndSetIfChanged method
                if (property.SetMethod is null)
                {
                    throw new Exception(
                        "[ReactiveProperty] is decorating "
                            + property.DeclaringType.FullName
                            + "."
                            + property.Name
                            + ", but the property has no setter so there would be nothing to react to.  Consider removing the attribute."
                    );
                }

                property.SetMethod.Body = new MethodBody(property.SetMethod);
                property.SetMethod.Body.Emit(il =>
                {
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldflda, field.BindDefinition(targetType)); // pop -> this.$PropertyName
                    il.Emit(OpCodes.Ldarg_1); // value
                    il.Emit(OpCodes.Ldstr, property.Name); // "PropertyName"
                    il.Emit(OpCodes.Call, methodReference); // pop * 4 -> this.RaiseAndSetIfChanged(this.$PropertyName, value, "PropertyName")
                    il.Emit(OpCodes.Pop); // We don't care about the result of RaiseAndSetIfChanged, so pop it off the stack (stack is now empty)
                    il.Emit(OpCodes.Ret); // Return out of the function
                });
            }
        }
    }
}
