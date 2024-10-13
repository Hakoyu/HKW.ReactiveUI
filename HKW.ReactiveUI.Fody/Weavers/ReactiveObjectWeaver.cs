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
internal class ReactiveObjectWeaver
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
        ModuleWeaver.Logger.LogInfo(nameof(ReactiveObjectWeaver));
        var w = new ReactiveObjectWeaver() { ModuleDefinition = moduleDefinition, };
        w.Execute();
    }

    public ModuleDefinition ModuleDefinition { get; private set; } = null!;

    public TypeReference ReactivePropertyAttribute { get; private set; } = null!;

    public TypeReference NotifyPropertyChangeFromAttribute { get; private set; } = null!;

    public void Execute()
    {
        ReactivePropertyAttribute =
            ModuleDefinition.FindType(
                "HKW.HKWReactiveUI",
                "ReactivePropertyAttribute",
                ModuleWeaver.HKWReactiveUI
            ) ?? throw new Exception("ReactivePropertyAttribute is null");

        NotifyPropertyChangeFromAttribute =
            ModuleDefinition.FindType(
                "HKW.HKWReactiveUI",
                "NotifyPropertyChangeFromAttribute",
                ModuleWeaver.HKWReactiveUI
            ) ?? throw new Exception("NotifyPropertyChangeFromAttribute is null");

        foreach (var classType in ModuleWeaver.IReactiveObjectDerivedClasses)
        {
            ExecuteClass(classType);
        }
    }

    public void ExecuteClass(TypeDefinition classType)
    {
        foreach (var property in classType.Properties)
        {
            ExecuteReactiveProperty(classType, property);
            ExecuteNotifyPropertyChangeFrom(classType, property);
        }
    }

    public void ExecuteNotifyPropertyChangeFrom(
        TypeDefinition classType,
        PropertyDefinition property
    )
    {
        if (property.IsDefined(NotifyPropertyChangeFromAttribute) is false)
            return;
        var fieldName = "_" + property.Name.FirstLetterToLower();
        var field =
            classType.Fields.FirstOrDefault(x => x.Name == fieldName)
            ?? throw new Exception($"Field {fieldName} not exist");

        property.GetMethod.Body = new MethodBody(property.GetMethod);
        property.GetMethod.Body.Emit(il =>
        {
            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldfld, field.BindDefinition(classType)); // pop -> this.$PropertyName
            il.Emit(OpCodes.Ret); // Return the field value that is lying on the stack
        });
    }

    public void ExecuteReactiveProperty(TypeDefinition classType, PropertyDefinition property)
    {
        if (property.IsDefined(ReactivePropertyAttribute) is false)
            return;
        if (property.SetMethod is null)
        {
            ModuleWeaver.Logger.LogError(
                $"Property {property.DeclaringType.FullName}.{property.Name} has no setter, therefore it is not possible for the property to change, and thus should not be marked with [ReactiveProperty]"
            );
            return;
        }

        var raiseAndSetMethodDefinition =
            classType
                .Resolve()
                .Methods.SingleOrDefault(x => x.Name == $"RaiseAndSet{property.Name}")
            ?? throw new Exception(
                $"{classType.FullName} not exists method RaiseAndSet{property.Name}, please check if you have added the partial keyword to class"
            );
        MethodReference raiseAndSetMethod = null!;
        TypeReference genericClassType = classType;

        if (classType.GenericParameters.Count > 0)
        {
            genericClassType = classType.MakeGenericInstanceType([.. classType.GenericParameters]);
            raiseAndSetMethod = raiseAndSetMethodDefinition.Bind(
                (GenericInstanceType)genericClassType
            );
            //raiseAndSetMethod = ModuleDefinition.ImportReference(m);
        }
        else
            raiseAndSetMethod = ModuleDefinition.ImportReference(raiseAndSetMethodDefinition);

        var check =
            property
                .CustomAttributes.First(x =>
                    x.AttributeType.FullName == ReactivePropertyAttribute.FullName
                )
                ?.ConstructorArguments[0]
                .Value
                is true;

        // 生成一个新字段, 命名为$PropertyName
        var field = new FieldDefinition(
            "$" + property.Name,
            FieldAttributes.Private,
            property.PropertyType
        );
        classType.Fields.Add(field);

        // 寻找旧字段并删除
        var oldField = (FieldReference)
            property.GetMethod.Body.Instructions.Single(x => x.Operand is FieldReference).Operand;
        var oldFieldDefinition = oldField.Resolve();
        classType.Fields.Remove(oldFieldDefinition);

        // 查看是否存在自动属性初始化器
        var constructors = classType.Methods.Where(x => x.IsConstructor);
        foreach (var constructor in constructors)
        {
            var fieldAssignment = constructor.Body.Instructions.SingleOrDefault(x =>
                Equals(x.Operand, oldFieldDefinition)
                || Equals(x.Operand?.ToString(), oldField.ToString())
            );
            if (fieldAssignment is not null)
            {
                //使用新字段初始化器替换自动生成的初始化器
                constructor
                    .Body.GetILProcessor()
                    .Replace(
                        fieldAssignment,
                        Instruction.Create(OpCodes.Ldflda, field.BindDefinition(classType))
                    );
            }
        }

        // 创建 getter
        property.GetMethod.Body = new MethodBody(property.GetMethod);
        property.GetMethod.Body.Emit(il =>
        {
            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldfld, field.BindDefinition(classType)); // pop -> this.$PropertyName
            il.Emit(OpCodes.Ret); // Return the field value that is lying on the stack
        });

        TypeReference genericTargetType = classType;
        if (classType.HasGenericParameters)
        {
            var genericDeclaration = new GenericInstanceType(classType);
            foreach (var parameter in classType.GenericParameters)
            {
                genericDeclaration.GenericArguments.Add(parameter);
            }

            genericTargetType = genericDeclaration;
        }

        // Build out the setter which fires the RaiseAndSet method
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

        // 创建新的setter
        property.SetMethod.Body = new MethodBody(property.SetMethod);
        //if (ModuleWeaver.ReactiveObjectX.IsAssignableFrom(classType))
        //{
        //var methodReference = raiseAndSetMethod.MakeGenericMethod(property.PropertyType);
        // 如果继承自 ReactiveObjectX 则使用 RaiseAndSet
        property.SetMethod.Body.Emit(il =>
        {
            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldflda, field.BindDefinition(classType)); // pop -> this.$PropertyName
            il.Emit(OpCodes.Ldarg_1); // value
            //il.Emit(OpCodes.Ldstr, property.Name); // "PropertyName"
            if (check) // Check
                il.Emit(OpCodes.Ldc_I4_1);
            else
                il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, raiseAndSetMethod); // pop * 4 -> this.RaiseAndSet(this.$PropertyName, value, "PropertyName")
            il.Emit(OpCodes.Nop); // Nop
            il.Emit(OpCodes.Ret); // Return out of the function
        });
        //}
        //else
        //{
        //    var methodReference = ModuleWeaver.RaiseAndSetIfChangedMethod.MakeGenericMethod(
        //        classType,
        //        property.PropertyType
        //    );
        //    // 否则使用传统方式 RaiseAndSetIfChanged
        //    property.SetMethod.Body.Emit(il =>
        //    {
        //        il.Emit(OpCodes.Ldarg_0); // this
        //        il.Emit(OpCodes.Ldarg_0); // this
        //        il.Emit(OpCodes.Ldflda, field.BindDefinition(classType)); // pop -> this.$PropertyName
        //        il.Emit(OpCodes.Ldarg_1); // value
        //        il.Emit(OpCodes.Ldstr, property.Name); // "PropertyName"
        //        il.Emit(OpCodes.Call, methodReference); // pop * 4 -> this.RaiseAndSetIfChanged(this.$PropertyName, value, "PropertyName")
        //        il.Emit(OpCodes.Pop); // We don't care about the result of RaiseAndSetIfChanged, so pop it off the stack (stack is now empty)
        //        il.Emit(OpCodes.Ret); // Return out of the function
        //    });
        //}
    }
}
