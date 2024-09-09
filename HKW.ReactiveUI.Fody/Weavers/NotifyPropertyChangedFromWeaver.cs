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
internal static class NotifyPropertyChangedFromWeaver
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
    /// [NotifyPropertyChangedFrom] is decorating " + property.DeclaringType.FullName + "." + property.Name + ", but the property has no setter so there would be nothing to react to.  Consider removing the attribute.
    /// </exception>
    public static void Execute(ModuleDefinition module)
    {
        ModuleWeaver.Logger.LogInfo(nameof(ReactivePropertyWeaver));

        var notifyPropertyChangeFromAttribute =
            module.FindType(
                "HKW.HKWReactiveUI",
                "NotifyPropertyChangeFromAttribute",
                ModuleWeaver.HKWReactiveUI
            ) ?? throw new Exception("NotifyPropertyChangeFromAttribute is null");
        foreach (var targetType in ModuleWeaver.IReactiveObjectDerivedClasses)
        {
            foreach (
                var property in targetType.Properties.Where(x =>
                    x.IsDefined(notifyPropertyChangeFromAttribute)
                )
            )
            {
                // Declare a field to store the property value
                var fieldName = "_" + property.Name.FirstLetterToLower();
                var field =
                    targetType.Fields.FirstOrDefault(x => x.Name == fieldName)
                    ?? throw new Exception($"Field {fieldName} not exist");
                // Build out the getter which simply returns the value of the generated field
                property.GetMethod.Body = new MethodBody(property.GetMethod);
                property.GetMethod.Body.Emit(il =>
                {
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldfld, field.BindDefinition(targetType)); // pop -> this.$PropertyName
                    il.Emit(OpCodes.Ret); // Return the field value that is lying on the stack
                });
            }
        }
    }
}
