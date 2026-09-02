using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace HKW.HKWReactiveUI.Fody;

internal sealed class ClassInfo
{
    public ClassInfo(TypeDefinition type)
    {
        Type = type;
        var helperPropertytName = type.Name + "ReactiveHelper";
        var helperObjectName = type.Name + "ReactiveObjectHelper";
        HelperType = type.NestedTypes.SingleOrDefault(x => x.Name == helperObjectName);
        HelperProperty = type.Properties.SingleOrDefault(x => x.Name == helperPropertytName);
        if (HelperType is null)
            throw new WeaverException($"Class {type.Name} has no {helperObjectName}");
    }

    public TypeDefinition Type { get; }

    public TypeDefinition HelperType { get; }
    public PropertyDefinition HelperProperty { get; }
}
