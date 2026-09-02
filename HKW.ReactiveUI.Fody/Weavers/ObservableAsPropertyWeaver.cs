using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace HKW.HKWReactiveUI.Fody;

/// <summary>
/// Weaver that converts observables as property helper.
/// </summary>
internal class ObservableAsPropertyWeaver
{
    public static void Weave(ClassInfo classInfo)
    {
        ModuleWeaver.Logger.LogInfo(nameof(ObservableAsPropertyWeaver));
        var w = new ObservableAsPropertyWeaver(classInfo);
        w.Weave();
    }

    private readonly ClassInfo _classInfo;
    private static MethodReference ObservableAsPropertyHelperGetValue { get; set; } = null!;

    public ObservableAsPropertyWeaver(ClassInfo classType)
    {
        _classInfo = classType;

        if (ObservableAsPropertyHelperGetValue is null)
        {
            ObservableAsPropertyHelperGetValue = WeaverHelper.ModuleDefinition.ImportReference(
                WeaverHelper
                    .ObservableAsPropertyHelper.Resolve()
                    .Properties.Single(x => x.Name == "Value")
                    .GetMethod
            );
        }
    }

    public void Weave()
    {
        foreach (var property in _classInfo.Type.Properties)
        {
            WeaveProperty(property);
        }
    }

    /// <summary>
    /// Executes this property weaver.
    /// </summary>
    public void WeaveProperty(PropertyDefinition property)
    {
        if (property.IsDefined(WeaverHelper.ObservableAsPropertyAttribute) is false)
            return;

        var genericObservableAsPropertyHelper =
            WeaverHelper.ObservableAsPropertyHelper.MakeGenericInstanceType(property.PropertyType);
        var genericObservableAsPropertyHelperGetValue = ObservableAsPropertyHelperGetValue.Bind(
            genericObservableAsPropertyHelper
        );
        WeaverHelper.ModuleDefinition.ImportReference(genericObservableAsPropertyHelperGetValue);

        var fieldName = "_" + property.Name.FirstLetterToLower() + "OAPH";
        var field =
            _classInfo.HelperType.Fields.FirstOrDefault(x => x.Name == fieldName)
            ?? throw new WeaverException($"Field {fieldName} not exist");

        property.GetMethod.Body = new MethodBody(property.GetMethod);
        property.GetMethod.Body.Emit(il =>
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // Helper
            il.Emit(OpCodes.Call, _classInfo.HelperProperty.GetMethod);
            // this.Helper._OAPH
            il.Emit(OpCodes.Ldfld, field.BindDefinition(_classInfo.HelperType));
            // this.Helper.
            il.Emit(OpCodes.Callvirt, genericObservableAsPropertyHelperGetValue);
            il.Emit(OpCodes.Ret); // Return the value that is on the stack
        });
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
        if (methodBody is null)
        {
            throw new ArgumentNullException(nameof(methodBody));
        }

        if (il is null)
        {
            throw new ArgumentNullException(nameof(il));
        }
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
