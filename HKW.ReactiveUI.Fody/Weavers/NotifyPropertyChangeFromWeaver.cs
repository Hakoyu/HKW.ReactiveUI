using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HKW.HKWReactiveUI.Fody;

internal class NotifyPropertyChangeFromWeaver
{
    public static void Weave(TypeDefinition classType)
    {
        ModuleWeaver.Logger.LogInfo(nameof(ReactivePropertyWeaver));
        var w = new ReactivePropertyWeaver(classType);
        w.Weave();
    }

    private readonly TypeDefinition _classType;

    public NotifyPropertyChangeFromWeaver(TypeDefinition obj)
    {
        _classType = obj;
    }

    public void Weave()
    {
        foreach (var property in _classType.Properties)
        {
            WeaveProperty(property);
        }
    }

    public void WeaveProperty(PropertyDefinition property)
    {
        if (property.IsDefined(WeaverHelper.NotifyPropertyChangeFromAttribute) is false)
            return;

        var attributeParameters = property
            .CustomAttributes.First(x =>
                x.AttributeType.FullName == WeaverHelper.NotifyPropertyChangeFromAttribute.FullName
            )
            .GetParams();
        if (attributeParameters.TryGetValue("CacheMode", out var cacheModeParameter))
        {
            var enableCache = cacheModeParameter.Value is not 0;
            if (enableCache is false)
                return;
            // 如果启用了缓存,则会生成一个新字段来缓存值
            var fieldName = "_" + property.Name.FirstLetterToLower();
            var field =
                _classType.Fields.FirstOrDefault(x => x.Name == fieldName)
                ?? throw new WeaverException($"Field {fieldName} not exist");

            property.GetMethod.Body = new MethodBody(property.GetMethod);
            property.GetMethod.Body.Emit(il =>
            {
                // this
                il.Emit(OpCodes.Ldarg_0);
                // this.$PropertyName
                il.Emit(OpCodes.Ldfld, field.BindDefinition(_classType));
                // Return
                il.Emit(OpCodes.Ret);
            });
        }
    }
}
