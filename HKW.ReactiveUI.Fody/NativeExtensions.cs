using System.Text;
using Microsoft.CodeAnalysis;
using Mono.Cecil;

namespace HKW.HKWReactiveUI.Fody;

internal static class NativeExtensions
{
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
    /// 获取特性构造参数
    /// </summary>
    /// <param name="customAttribute">特性</param>
    /// <returns>参数字典 (ArgsName, ArgsValue)</returns>
    public static Dictionary<string, AttributeParameterValue> GetAttributeParameters(
        this CustomAttribute? customAttribute
    )
    {
        var parameters = new Dictionary<string, AttributeParameterValue>();
        if (customAttribute is null)
            return parameters;
        // 分析构造,但ConstructorArguments重没有参数名称,所以从Resolve获取参数名称
        foreach (
            var (argument, parameter) in customAttribute.ConstructorArguments.Zip(
                customAttribute.Constructor.Resolve().Parameters,
                (argument, parameter) => (argument, parameter)
            )
        )
        {
            parameters.Add(parameter.Name, new(argument));
        }
        // 获取构造的字段
        foreach (var field in customAttribute.Fields)
        {
            parameters.Add(field.Name, new(field.Argument));
        }
        // 获取构造的属性
        foreach (var property in customAttribute.Properties)
        {
            parameters.Add(property.Name, new(property.Argument));
        }
        return parameters;
    }
}

/// <summary>
/// 特性参数值
/// </summary>
internal class AttributeParameterValue
{
    public AttributeParameterValue(CustomAttributeArgument argument)
    {
        if (argument.Value is CustomAttributeArgument[] args)
            Values = args.Select(x => x.Value).ToArray();
        else
            Value = argument.Value;
    }

    public object? Value { get; set; } = null;
    public object?[]? Values { get; set; } = null;
}
