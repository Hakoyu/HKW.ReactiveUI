// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace HKW.HKWReactiveUI.Fody;

/// <summary>
/// Weaver that converts observables as property helper.
/// </summary>
internal static class ObservableAsPropertyWeaver
{
    /// <summary>
    /// Executes this property weaver.
    /// </summary>
    public static void Execute(
        ModuleDefinition moduleDefinition,
        Func<string, TypeDefinition> findType
    )
    {
        ModuleWeaver.Logger.LogInfo(nameof(ObservableAsPropertyWeaver));

        var exceptionName = typeof(Exception).FullName;

        if (exceptionName is null)
        {
            ModuleWeaver.Logger.LogError("Could not find the full name for System.Exception");
            return;
        }

        var observableAsPropertyHelper = moduleDefinition.FindType(
            "ReactiveUI",
            "ObservableAsPropertyHelper`1",
            ModuleWeaver.ReactiveUI,
            "T"
        );
        var observableAsPropertyAttribute = moduleDefinition.FindType(
            "HKW.HKWReactiveUI",
            "ObservableAsPropertyAttribute",
            ModuleWeaver.HKWReactiveUI
        );
        var observableAsPropertyHelperGetValue = moduleDefinition.ImportReference(
            observableAsPropertyHelper.Resolve().Properties.Single(x => x.Name == "Value").GetMethod
        );
        var exceptionDefinition = findType.Invoke(exceptionName);
        var constructorDefinition = exceptionDefinition
            .GetConstructors()
            .Single(x => x.Parameters.Count == 1);
        var exceptionConstructor = moduleDefinition.ImportReference(constructorDefinition);
        foreach (var targetType in ModuleWeaver.IReactiveObjectDerivedClasses)
        {
            foreach (
                var property in targetType.Properties.Where(x =>
                    x.IsDefined(observableAsPropertyAttribute)
                    || (x.GetMethod?.IsDefined(observableAsPropertyAttribute) ?? false)
                )
            )
            {
                var genericObservableAsPropertyHelper =
                    observableAsPropertyHelper.MakeGenericInstanceType(property.PropertyType);
                var genericObservableAsPropertyHelperGetValue =
                    observableAsPropertyHelperGetValue.Bind(genericObservableAsPropertyHelper);
                moduleDefinition.ImportReference(genericObservableAsPropertyHelperGetValue);

                // Declare a field to store the property value
                var field = new FieldDefinition(
                    "$" + property.Name,
                    FieldAttributes.Private,
                    genericObservableAsPropertyHelper
                );
                targetType.Fields.Add(field);

                // It's an auto-property, so remove the generated field
                if (property.SetMethod?.HasBody == true)
                {
                    // Remove old field (the generated backing field for the auto property)
                    var oldField = (FieldReference)
                        property
                            .GetMethod.Body.Instructions.Single(x => x.Operand is FieldReference)
                            .Operand;
                    var oldFieldDefinition = oldField.Resolve();
                    targetType.Fields.Remove(oldFieldDefinition);

                    // Re-implement setter to throw an exception
                    property.SetMethod.Body = new MethodBody(property.SetMethod);
                    property.SetMethod.Body.Emit(il =>
                    {
                        il.Emit(
                            OpCodes.Ldstr,
                            "Never call the setter of an ObservableAsPropertyHelper property."
                        );
                        il.Emit(OpCodes.Newobj, exceptionConstructor);
                        il.Emit(OpCodes.Throw);
                        il.Emit(OpCodes.Ret);
                    });
                }

                property.GetMethod.Body = new MethodBody(property.GetMethod);
                property.GetMethod.Body.Emit(il =>
                {
                    var isValid = il.Create(OpCodes.Nop);
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldfld, field.BindDefinition(targetType)); // pop -> this.$PropertyName
                    il.Emit(OpCodes.Dup); // Put an extra copy of this.$PropertyName onto the stack
                    il.Emit(OpCodes.Brtrue, isValid); // If the helper is null, return the default value for the property
                    il.Emit(OpCodes.Pop); // Drop this.$PropertyName
                    EmitDefaultValue(
                        moduleDefinition,
                        property.GetMethod.Body,
                        il,
                        property.PropertyType
                    ); // Put the default value onto the stack
                    il.Emit(OpCodes.Ret); // Return that default value
                    il.Append(isValid); // Add a marker for if the helper is not null
                    il.Emit(OpCodes.Callvirt, genericObservableAsPropertyHelperGetValue); // pop -> this.$PropertyName.Value
                    il.Emit(OpCodes.Ret); // Return the value that is on the stack
                });
            }
        }
    }

    /// <summary>
    /// Emits the default value.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    /// <param name="il">The il.</param>
    /// <param name="type">The type.</param>
    public static void EmitDefaultValue(
        ModuleDefinition moduleDefinition,
        MethodBody methodBody,
        ILProcessor il,
        TypeReference type
    )
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(methodBody);
        ArgumentNullException.ThrowIfNull(il);
#else
        if (methodBody is null)
        {
            throw new ArgumentNullException(nameof(methodBody));
        }

        if (il is null)
        {
            throw new ArgumentNullException(nameof(il));
        }
#endif

        if (moduleDefinition is not null)
        {
            if (
                type.CompareTo(moduleDefinition.TypeSystem.Boolean)
                || type.CompareTo(moduleDefinition.TypeSystem.Byte)
                || type.CompareTo(moduleDefinition.TypeSystem.Int16)
                || type.CompareTo(moduleDefinition.TypeSystem.Int32)
            )
            {
                il.Emit(OpCodes.Ldc_I4_0);
            }
            else if (type.CompareTo(moduleDefinition.TypeSystem.Single))
            {
                il.Emit(OpCodes.Ldc_R4, 0F);
            }
            else if (type.CompareTo(moduleDefinition.TypeSystem.Int64))
            {
                il.Emit(OpCodes.Ldc_I8, 0L);
            }
            else if (type.CompareTo(moduleDefinition.TypeSystem.Double))
            {
                il.Emit(OpCodes.Ldc_R8, 0D);
            }
            else if (type.IsGenericParameter || type.IsValueType)
            {
                methodBody.InitLocals = true;
                var local = new VariableDefinition(type);
                il.Body.Variables.Add(local);
                il.Emit(OpCodes.Ldloca_S, local);
                il.Emit(OpCodes.Initobj, type);
                il.Emit(OpCodes.Ldloc, local);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
        }
    }
}
