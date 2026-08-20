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
    /// 启用, 在对象初始化时缓存
    /// </summary>
    Enable,

    /// <summary>
    /// 在初始化后启用, 在目标属性被调用后才缓存
    /// </summary>
    AfterInitialize,
}
