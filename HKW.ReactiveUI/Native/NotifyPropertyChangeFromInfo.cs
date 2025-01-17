using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

/// <summary>
/// 方法属性信息
/// </summary>
/// <param name="propertyName">属性名</param>
/// <param name="propertyType">属性类型</param>
/// <param name="builder">方法</param>
/// <param name="cacheAtInitialize">初始化值时发送通知</param>
/// <param name="staticAction">静态行动</param>
public class NotifyPropertyChangeFromInfo(
    string propertyName,
    ITypeSymbol propertyType,
    StringBuilder builder,
    bool cacheAtInitialize,
    bool staticAction
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
    /// 初始化时缓存
    /// </summary>
    public bool CacheAtInitialize { get; set; } = cacheAtInitialize;

    /// <summary>
    /// 启用缓存
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// 静态行动
    /// </summary>
    public bool StaticAction { get; set; } = staticAction;

    /// <summary>
    /// 数据
    /// </summary>
    public StringBuilder Builder { get; set; } = builder;

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
