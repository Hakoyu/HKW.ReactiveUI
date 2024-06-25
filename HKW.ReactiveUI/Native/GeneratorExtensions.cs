using System.Collections.Immutable;
using HKW.HKWReactiveUI.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HKW.HKWReactiveUI;

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
            is not ImmutableArray<IParameterSymbol> constructorParams
        )
            return false;

        var allArguments = attributeData
            .ConstructorArguments
            // 如果参数未命名,则从参数顺序获取名称
            .Select((info, index) => (constructorParams[index].Name, info))
            // 然后合并参数和值
            .Union(attributeData.NamedArguments.Select(x => (x.Key, x.Value)))
            .Distinct();

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
    /// <param name="typeSymbol">类型符号</param>
    /// <param name="baseTypeFullName">基类全名</param>
    /// <param name="symbolDisplayFormat">显示名称格式</param>
    /// <returns>有任意基类全名是为 <see langword="true"/> 没有为 <see langword="false"/></returns>
    public static bool AnyBaseTypeIs(
        this ITypeSymbol typeSymbol,
        string baseTypeFullName,
        SymbolDisplayFormat? symbolDisplayFormat = null
    )
    {
        var currentType = typeSymbol;
        while (currentType != null)
        {
            if (
                (
                    symbolDisplayFormat is null
                        ? currentType.ToString()
                        : currentType.ToDisplayString(symbolDisplayFormat)
                ) == baseTypeFullName
            )
                return true;
            currentType = currentType.BaseType;
        }
        return false;
    }

    /// <summary>
    /// 获取任务返回值类型
    /// </summary>
    /// <param name="compilation">编译</param>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>返回值类型</returns>
    public static ITypeSymbol GetTaskReturnType(
        this Compilation compilation,
        ITypeSymbol typeSymbol
    )
    {
        if (typeSymbol is INamedTypeSymbol { TypeArguments.Length: 1 } namedTypeSymbol)
            return namedTypeSymbol.TypeArguments[0];
        return compilation.GetVoid();
    }

    public static SyntaxToken GetAccessibility(this SyntaxTokenList syntaxes)
    {
        var accessibility = syntaxes.FirstOrDefault(m =>
            m.IsKind(SyntaxKind.PublicKeyword)
            || m.IsKind(SyntaxKind.InternalKeyword)
            || m.IsKind(SyntaxKind.PrivateKeyword)
        );
        return accessibility;
    }

    /// <summary>
    /// 是空类型
    /// </summary>
    /// <param name="compilation">编译</param>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns></returns>
    public static bool IsVoid(this Compilation compilation, ITypeSymbol typeSymbol)
    {
        return SymbolEqualityComparer.Default.Equals(typeSymbol, compilation.GetVoid());
    }

    /// <summary>
    /// 获取空类型
    /// </summary>
    /// <param name="compilation"></param>
    /// <returns></returns>
    public static ITypeSymbol GetVoid(this Compilation compilation)
    {
        return compilation.GetSpecialType(SpecialType.System_Void);
    }
}
