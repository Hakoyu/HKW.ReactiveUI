using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

/// <summary>
/// 方法属性信息
/// </summary>
/// <param name="propertyName">属性名</param>
/// <param name="propertyType">属性类型</param>
/// <param name="builder">方法</param>
/// <param name="notifyOnInitialValue">初始化值时发送通知</param>
public class NotifyPropertyChangeFromInfo(
    string propertyName,
    ITypeSymbol propertyType,
    StringBuilder builder,
    bool notifyOnInitialValue
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
    /// 初始化值时发送通知
    /// </summary>
    public bool NotifyOnInitialValue { get; set; } = notifyOnInitialValue;

    ///// <summary>
    ///// 是主体表达式
    ///// </summary>
    //public bool IsBodied { get; set; } = isBodied;

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
