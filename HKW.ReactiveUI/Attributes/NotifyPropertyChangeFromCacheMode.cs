namespace HKW.HKWReactiveUI;

/// <summary>
/// 缓存模式
/// </summary>
public enum NotifyPropertyChangeFromCacheMode
{
    /// <summary>
    /// 禁用
    /// </summary>
    Disable,

    /// <summary>
    /// 启用, 并在对象初始化时缓存
    /// </summary>
    Enable,

    /// <summary>
    /// 首次关联属性变更时才计算并缓存。
    /// </summary>
    OnFirstChange,
}
