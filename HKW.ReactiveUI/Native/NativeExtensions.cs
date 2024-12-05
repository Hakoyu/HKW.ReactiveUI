using System.Collections.Immutable;
using System.Text;
using HKW.HKWReactiveUI.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HKW.HKWReactiveUI;

internal static class NativeExtensions
{
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
            // 如果是多值 (params) 则添加多值
            if (info.Kind is TypedConstantKind.Array)
            {
                parameters.Add(name, new(info.Values));
            }
            else
            {
                parameters.Add(name, new(info));
            }
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
