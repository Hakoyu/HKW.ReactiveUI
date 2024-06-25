﻿using System.Collections.Immutable;
using HKW.HKWReactiveUI.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI.Extensions;

internal static class GeneratorExtensions
{
    /// <summary>
    /// 尝试获取属性的值
    /// </summary>
    /// <param name="attributeData">属性数据</param>
    /// <param name="attributeValues">属性值</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool TryGetAttributeAndValues(
        this AttributeData attributeData,
        out List<NameTypeAndValue> attributeValues
    )
    {
        attributeValues = [];
        if (
            attributeData.AttributeConstructor?.Parameters
            is ImmutableArray<IParameterSymbol> constructorParams
        )
            return false;
        // Start with an indexed list of names for mandatory args

        var allArguments = attributeData
            .ConstructorArguments
            // For unnamed args, we get the name from the array we just made
            .Select((info, index) => (constructorParams[index].Name, info))
            // Then we use name + value from the named values
            .Union(attributeData.NamedArguments.Select(x => (x.Key, x.Value)))
            .Distinct()
            .ToArray();

        foreach (var (name, info) in allArguments)
        {
            attributeValues.Add(
                new NameTypeAndValue(name: name, typeFullName: info.Type!.Name, value: info.Value!)
            );
        }
        if (attributeValues.Count == 0)
            return false;
        return true;
    }

    /// <summary>
    /// 任意基类是
    /// </summary>
    /// <param name="namedTypeSymbol">命名类型符号</param>
    /// <param name="baseTypeFullName">基类全名</param>
    /// <returns>有任意基类全名是为 <see langword="true"/> 没有为 <see langword="false"/></returns>
    public static bool AnyBaseTypeIs(this INamedTypeSymbol namedTypeSymbol, string baseTypeFullName)
    {
        var currentType = namedTypeSymbol;
        while (currentType != null)
        {
            if (currentType.ToString() == baseTypeFullName)
                return true;
            currentType = currentType.BaseType;
        }
        return false;
    }
}
