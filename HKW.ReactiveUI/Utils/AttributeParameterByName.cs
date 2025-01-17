using System.Collections;

namespace HKW.SourceGeneratorUtils;

/// <summary>
/// 特性参数
/// </summary>
public class AttributeParameterByName : Dictionary<string, object?>
{
    /// <summary>
    /// 尝试获取值
    /// </summary>
    /// <typeparam name="TOutValue">类型</typeparam>
    /// <param name="parameterName">参数名称</param>
    /// <param name="outValue"></param>
    /// <returns>参数存在为 <see langword="true"/> 不存在为 <see langword="false"/></returns>
    public bool TryGetValue<TOutValue>(string parameterName, out TOutValue outValue)
    {
        var r = base.TryGetValue(parameterName, out var value);
        if (value is null)
            outValue = default!;
        else
            outValue = (TOutValue)value!;
        return r;
    }

    /// <summary>
    /// 尝试获取数组
    /// </summary>
    /// <typeparam name="TOutValue">类型</typeparam>
    /// <param name="parameterName">参数名称</param>
    /// <param name="outValue"></param>
    /// <returns>参数存在为 <see langword="true"/> 不存在为 <see langword="false"/></returns>
    public bool TryGetArray<TOutValue>(string parameterName, out TOutValue[] outValue)
    {
        var r = base.TryGetValue(parameterName, out var value);
        if (value is ICollection collection)
            outValue = collection.Cast<TOutValue>().ToArray();
        else
            outValue = default!;
        return r;
    }
}
