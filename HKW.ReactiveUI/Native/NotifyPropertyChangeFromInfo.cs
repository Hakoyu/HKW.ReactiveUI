using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

/// <summary>
/// 方法属性信息
/// </summary>
/// <param name="propertyName">属性名</param>
/// <param name="propertyType">属性类型</param>
/// <param name="getMethod">方法</param>
/// <param name="params">参数</param>
public sealed class NotifyPropertyChangeFromInfo(
    string propertyName,
    ITypeSymbol propertyType,
    string getMethod,
    string[] @params
) : IEquatable<NotifyPropertyChangeFromInfo>
{
    /// <summary>
    /// 属性名
    /// </summary>
    public string PropertyName { get; set; } = propertyName;

    /// <summary>
    /// 属性类型
    /// </summary>
    public ITypeSymbol Type { get; set; } = propertyType;

    /// <summary>
    /// 启用缓存
    /// </summary>
    public NotifyPropertyChangeFromCacheMode CacheMode { get; set; }

    /// <summary>
    /// Get方法
    /// </summary>
    public string GetMethod { get; set; } = getMethod;

    /// <summary>
    /// 参数
    /// </summary>
    public string[] Params { get; set; } = @params;

    #region IEquatable
    /// <inheritdoc/>
    public bool Equals(NotifyPropertyChangeFromInfo? other)
    {
        if (other is null)
            return false;
        return PropertyName == other.PropertyName;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as NotifyPropertyChangeFromInfo);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return PropertyName.GetHashCode();
    }
    #endregion
}
