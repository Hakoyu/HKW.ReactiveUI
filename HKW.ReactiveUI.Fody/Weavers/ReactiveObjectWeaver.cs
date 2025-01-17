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
    /// 执行
    /// </summary>
    /// <param name="moduleDefinition"></param>
    public static void Execute(ModuleDefinition moduleDefinition)
    {
        ModuleWeaver.Logger.LogInfo(nameof(ReactiveObjectWeaver));
        var w = new ReactiveObjectWeaver() { ModuleDefinition = moduleDefinition, };
        w.Execute();
    }

    public ModuleDefinition ModuleDefinition { get; private set; } = null!;

    public TypeReference ReactivePropertyAttribute { get; private set; } = null!;

    public TypeReference NotifyPropertyChangeFromAttribute { get; private set; } = null!;

    public TypeReference ObservableAsPropertyAttribute { get; private set; } = null!;

    public TypeReference ObservableAsPropertyHelper { get; private set; } = null!;

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

        ObservableAsPropertyAttribute =
            ModuleDefinition.FindType(
                "HKW.HKWReactiveUI",
                "ObservableAsPropertyAttribute",
                ModuleWeaver.HKWReactiveUI
            ) ?? throw new Exception("ObservableAsPropertyAttribute is null");

        ObservableAsPropertyHelper = ModuleDefinition.FindType(
            "ReactiveUI",
            "ObservableAsPropertyHelper`1",
            ModuleWeaver.ReactiveUI,
            "T"
        );

        foreach (var classType in ModuleWeaver.IReactiveObjectDerivedClasses)
        {
            ClassWeaver(classType);
        }
    }

    public void ClassWeaver(TypeDefinition classType)
    {
        foreach (var property in classType.Properties)
        {
            ReactivePropertyWeaver(classType, property);
            NotifyPropertyChangeFromWeaver(classType, property);
            ObservableAsPropertyWeaver(classType, property);
        }
    }

    public void ObservableAsPropertyWeaver(TypeDefinition classType, PropertyDefinition property)
    {
        if (property.IsDefined(ObservableAsPropertyAttribute) is false)
            return;

        // 如果启用了缓存,则会生成一个新字段来缓存值
        var fieldName = "_" + property.Name.FirstLetterToLower();
        var field =
            classType.Fields.FirstOrDefault(x => x.Name == fieldName)
            ?? throw new Exception($"Field {fieldName} not exist");
        var genericHelper = ObservableAsPropertyHelper.MakeGenericInstanceType(
            property.PropertyType
        );
        var helperGetValue = ModuleDefinition.ImportReference(
            ObservableAsPropertyHelper.Resolve().Properties.Single(x => x.Name == "Value").GetMethod
        );
        var genericHelperGetValue = helperGetValue.Bind(genericHelper);
        property.GetMethod.Body = new MethodBody(property.GetMethod);
        property.GetMethod.Body.Emit(il =>
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this.$PropertyName
            il.Emit(OpCodes.Ldfld, field.BindDefinition(classType));
            // field.Value
            il.Emit(OpCodes.Callvirt, genericHelperGetValue);
            // Return
            il.Emit(OpCodes.Ret);
        });
    }

    public void NotifyPropertyChangeFromWeaver(
        TypeDefinition classType,
        PropertyDefinition property
    )
    {
        if (property.IsDefined(NotifyPropertyChangeFromAttribute) is false)
            return;

        var attributeParameters = property
            .CustomAttributes.First(x =>
                x.AttributeType.FullName == NotifyPropertyChangeFromAttribute.FullName
            )
            .GetAttributeParameters();
        attributeParameters.TryGetValue("EnableCache", out var enableCacheParameter);
        var enableCache = enableCacheParameter?.Value is not false;

        if (enableCache is false)
            return;
        // 如果启用了缓存,则会生成一个新字段来缓存值
        var fieldName = "_" + property.Name.FirstLetterToLower();
        var field =
            classType.Fields.FirstOrDefault(x => x.Name == fieldName)
            ?? throw new Exception($"Field {fieldName} not exist");

        property.GetMethod.Body = new MethodBody(property.GetMethod);
        property.GetMethod.Body.Emit(il =>
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this.$PropertyName
            il.Emit(OpCodes.Ldfld, field.BindDefinition(classType));
            // Return
            il.Emit(OpCodes.Ret);
        });
    }

    public void ReactivePropertyWeaver(TypeDefinition classType, PropertyDefinition property)
    {
        if (property.IsDefined(ReactivePropertyAttribute) is false)
            return;

        // 如果没有SetMethod
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
        var isGenericClass = classType.GenericParameters.Count > 0;
        if (isGenericClass)
        {
            // 如果是泛型类型,需要拼接一个完整的泛型类型
            var genericClassType = classType.MakeGenericInstanceType(
                [.. classType.GenericParameters]
            );
            // 这样会生成Class`1<T>::Method 而不是 Class`1::Method, 后者在IL引用中会出错
            raiseAndSetMethod = raiseAndSetMethodDefinition.Bind(genericClassType);
        }
        else
            raiseAndSetMethod = ModuleDefinition.ImportReference(raiseAndSetMethodDefinition);

        var attributeParameters = property
            .CustomAttributes.First(x =>
                x.AttributeType.FullName == ReactivePropertyAttribute.FullName
            )
            .GetAttributeParameters();
        attributeParameters.TryGetValue("Check", out var checkParameter);
        // 获取检查属性
        var check = checkParameter?.Value is not false;

        // 生成一个新字段, 命名为 $PropertyName
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
            if (fieldAssignment is null)
                continue;

            //使用新字段初始化器替换自动生成的初始化器
            if (isGenericClass)
            {
                constructor
                    .Body.GetILProcessor()
                    .Replace(
                        fieldAssignment,
                        Instruction.Create(fieldAssignment.OpCode, field.BindDefinition(classType))
                    );
            }
            else
            {
                constructor
                    .Body.GetILProcessor()
                    .Replace(fieldAssignment, Instruction.Create(OpCodes.Stfld, field));
            }
        }

        // 创建 getter
        property.GetMethod.Body = new MethodBody(property.GetMethod);
        property.GetMethod.Body.Emit(il =>
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this.$PropertyName
            il.Emit(OpCodes.Ldfld, field.BindDefinition(classType));
            // Return the field value that is lying on the stack
            il.Emit(OpCodes.Ret);
        });

        // 创建新的setter
        property.SetMethod.Body = new MethodBody(property.SetMethod);
        property.SetMethod.Body.Emit(il =>
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // ref field
            il.Emit(OpCodes.Ldflda, field.BindDefinition(classType));
            // newValue
            il.Emit(OpCodes.Ldarg_1);
            if (check) // Check
                il.Emit(OpCodes.Ldc_I4_1);
            else
                il.Emit(OpCodes.Ldc_I4_0);
            // this.RaiseAndSetProperty(ref field, newValue, check)
            il.Emit(OpCodes.Call, raiseAndSetMethod);
            // Nop
            il.Emit(OpCodes.Nop);
            // Return out of the function
            il.Emit(OpCodes.Ret);
        });
    }
}
