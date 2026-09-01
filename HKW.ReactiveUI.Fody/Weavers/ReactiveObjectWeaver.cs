using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace HKW.HKWReactiveUI.Fody;

/// <summary>
///
/// </summary>
internal class ReactiveObjectWeaver
{
    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="moduleDefinition"></param>
    public static void Weave(ModuleDefinition moduleDefinition, TypeDefinition obj)
    {
        if (
            obj.BaseType is null
            || obj.BaseType.IsAssignableFrom(WeaverHelper.IReactiveObject) is false
        )
            return;
        ModuleWeaver.Logger.LogInfo(nameof(ReactiveObjectWeaver));
        var w = new ReactiveObjectWeaver(obj) { };
        w.Weave();
    }

    private TypeDefinition _object;

    public ReactiveObjectWeaver(TypeDefinition obj)
    {
        _object = obj;
    }

    public void Weave()
    {
        foreach (var property in _object.Properties)
        {
            //ReactivePropertyWeaver(_object, property);
            //NotifyPropertyChangeFromWeaver(classType, property);
            //ObservableAsPropertyWeaver(classType, property);
        }
    }

    public void ObservableAsPropertyWeaver(TypeDefinition classType, PropertyDefinition property)
    {
        //if (property.IsDefined(ObservableAsPropertyAttribute) is false)
        //    return;

        //// 如果启用了缓存,则会生成一个新字段来缓存值
        //var fieldName = "_" + property.Name.FirstLetterToLower();
        //var field =
        //    classType.Fields.FirstOrDefault(x => x.Name == fieldName)
        //    ?? throw new WeaverException($"Field {fieldName} not exist");
        //var genericHelper = ObservableAsPropertyHelper.MakeGenericInstanceType(
        //    property.PropertyType
        //);
        //var helperGetValue = ModuleDefinition.ImportReference(
        //    ObservableAsPropertyHelper.Resolve().Properties.Single(x => x.Name == "Value").GetMethod
        //);
        //var genericHelperGetValue = helperGetValue.Bind(genericHelper);
        //property.GetMethod.Body = new MethodBody(property.GetMethod);
        //property.GetMethod.Body.Emit(il =>
        //{
        //    // this
        //    il.Emit(OpCodes.Ldarg_0);
        //    // this.$PropertyName
        //    il.Emit(OpCodes.Ldfld, field.BindDefinition(classType));
        //    // field.Value
        //    il.Emit(OpCodes.Callvirt, genericHelperGetValue);
        //    // Return
        //    il.Emit(OpCodes.Ret);
        //});
    }
}
