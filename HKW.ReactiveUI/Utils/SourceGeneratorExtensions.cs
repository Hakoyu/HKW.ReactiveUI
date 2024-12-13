﻿using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HKW.SourceGeneratorUtils;

internal static class SourceGeneratorExtensions
{
    /// <summary>
    /// 从当前类以及父类中获取成员
    /// </summary>
    /// <typeparam name="TMemberType">成员类型</typeparam>
    /// <param name="symbol">当前类</param>
    /// <param name="memberName">成员名称</param>
    /// <returns>成员</returns>
    public static TMemberType? GetMember<TMemberType>(
        this INamedTypeSymbol symbol,
        string memberName
    )
        where TMemberType : ISymbol
    {
        var temp = symbol;
        var member = temp.GetMembers(memberName).OfType<TMemberType>().FirstOrDefault();
        while (member is null && temp.BaseType is not null)
        {
            temp = temp.BaseType;
            member = temp.GetMembers(memberName).OfType<TMemberType>().FirstOrDefault();
        }
        return member;
    }

    /// <summary>
    /// 获取全名
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    public static string GetFullName(this ITypeSymbol typeSymbol)
    {
        return $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}";
    }

    /// <summary>
    /// 获取名称和泛型
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <param name="xmlFormat">将泛型符号 <c>&lt;&gt;</c> 替换为 <c>{}</c></param>
    /// <returns></returns>
    public static string GetNameAndGeneric(this ITypeSymbol typeSymbol, bool xmlFormat = false)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (xmlFormat)
                return $"{namedTypeSymbol.Name}{(namedTypeSymbol.TypeParameters.Length > 0 ? $"{{{string.Join(", ", namedTypeSymbol.TypeParameters)}}}" : string.Empty)}";
            return $"{namedTypeSymbol.Name}{(namedTypeSymbol.TypeParameters.Length > 0 ? $"<{string.Join(", ", namedTypeSymbol.TypeParameters)}>" : string.Empty)}";
        }
        return typeSymbol.GetFullName();
    }

    /// <summary>
    /// 获取全面和泛型
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <param name="xmlFormat">将泛型符号 <c>&lt;&gt;</c> 替换为 <c>{}</c></param>
    /// <returns></returns>
    public static string GetFullNameAndGeneric(this ITypeSymbol typeSymbol, bool xmlFormat = false)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (xmlFormat)
                return $"{namedTypeSymbol}{(namedTypeSymbol.TypeParameters.Length > 0 ? $"{{{string.Join(", ", namedTypeSymbol.TypeParameters)}}}" : string.Empty)}";
            return $"{namedTypeSymbol}{(namedTypeSymbol.TypeParameters.Length > 0 ? $"<{string.Join(", ", namedTypeSymbol.TypeParameters)}>" : string.Empty)}";
        }
        return typeSymbol.GetFullName();
    }

    /// <summary>
    /// 获取最低访问性字符串
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <param name="typeSymbols"></param>
    /// <returns></returns>
    public static string GetLowestAccessibility(
        this INamedTypeSymbol typeSymbol,
        params INamedTypeSymbol[] typeSymbols
    )
    {
        var lower = typeSymbol;
        foreach (var symbol in typeSymbols)
            lower = lower.DeclaredAccessibility > symbol.DeclaredAccessibility ? symbol : lower;
        return lower.GetAccessibilityString();
    }

    /// <summary>
    /// 获取访问性字符串
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    public static string GetAccessibilityString(this INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.Internal and Accessibility.Friend => "internal",
            Accessibility.Public => "public",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal
            and Accessibility.ProtectedAndFriend
                => "protected internal",
            Accessibility.ProtectedOrInternal
            and Accessibility.ProtectedOrFriend
                => "protected internal",
            _ => ""
        };
    }

    public static string ToXMLFormat(this string str)
    {
        var chars = str.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            switch (chars[i])
            {
                case '<':
                    chars[i] = '{';
                    break;
                case '>':
                    chars[i] = '}';
                    break;
                default:
                    continue;
            }
        }
        return new string(chars);
    }

    /// <summary>
    /// 获取特性参数值
    /// </summary>
    /// <param name="attributeData"></param>
    /// <returns>特性参数和值 (ParameterName, ParameterValue)</returns>
    public static Dictionary<string, AttributeParameterValue> GetAttributeParameters(
        this AttributeData attributeData
    )
    {
        var parameters = new Dictionary<string, AttributeParameterValue>();
        if (
            attributeData?.AttributeConstructor?.Parameters
            is not ImmutableArray<IParameterSymbol> constructorParams
        )
            return parameters;

        var allArguments = attributeData
            .ConstructorArguments
            // 如果参数未命名,则从参数顺序获取名称
            .Select((info, index) => (constructorParams[index].Name, info))
            // 然后合并参数和值
            .Union(attributeData.NamedArguments.Select(x => (x.Key, x.Value)))
            .Distinct();

        foreach (var (name, info) in allArguments)
        {
            parameters.Add(name, new(info));
        }
        return parameters;
    }

    /// <summary>
    /// 继承自基类类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <param name="baseTypeFullName">基类全名</param>
    /// <param name="symbolDisplayFormat">显示名称格式</param>
    /// <returns>当类型继承基类时为 <see langword="true"/> 未继承为 <see langword="false"/></returns>
    public static bool InheritedFrom(
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

    /// <summary>
    /// 获取可访问性
    /// </summary>
    /// <param name="syntaxes"></param>
    /// <returns></returns>
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
    /// <returns>IsVoid</returns>
    public static bool IsVoid(this Compilation compilation, ITypeSymbol typeSymbol)
    {
        return SymbolEqualityComparer.Default.Equals(typeSymbol, compilation.GetVoid());
    }

    /// <summary>
    /// 获取空类型
    /// </summary>
    /// <param name="compilation"></param>
    /// <returns>VoidType</returns>
    public static ITypeSymbol GetVoid(this Compilation compilation)
    {
        return compilation.GetSpecialType(SpecialType.System_Void);
    }

    /// <summary>
    /// 首字母小写
    /// </summary>
    /// <param name="str"></param>
    /// <returns>首字母为小写的字符串</returns>
    public static string FirstLetterToLower(this string str)
    {
        if (string.IsNullOrWhiteSpace(str) || char.IsLower(str, 0))
            return str;
        var array = str.ToCharArray();
        array[0] = char.ToLowerInvariant(array[0]);
        return new string(array);
    }

    /// <summary>
    /// 获取Get方法
    /// </summary>
    /// <param name="propertySymbol">属性</param>
    /// <param name="staticAction">使用自身静态方法</param>
    /// <returns>属性Get方法信息</returns>
    public static StringBuilder? GetGetMethodInfo(
        this IPropertySymbol propertySymbol,
        out bool staticAction
    )
    {
        staticAction = false;
        if (propertySymbol.GetMethod is null)
            return null;
        var getMethod = propertySymbol
            .GetMethod.DeclaringSyntaxReferences.First()
            .GetSyntax()
            .ToString();
        var sb = new StringBuilder(getMethod);
        var isBodied = getMethod.Last() != '}';
        if (isBodied)
        {
            staticAction = getMethod.IndexOf("this.To(static") > 0;
            sb.Remove(0, getMethod.IndexOf("=>") + 2);
            if (staticAction)
            {
                sb.Replace("this.To", "_this.To");
            }
        }
        return sb;
    }
}
