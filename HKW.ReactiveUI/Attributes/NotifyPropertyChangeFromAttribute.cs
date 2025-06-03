namespace HKW.HKWReactiveUI;

/// <summary>
/// 从目标属性通知当前属性改变
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [NotifyPropertyChangeFrom(true, nameof(ID), nameof(Name), EnableCache = true)]
///     public string IsSame => ID == Name;
///
///     protected void InitializeReactiveObject() { }
/// }
/// ]]></code>
/// </para>
/// 这样就会生成代码
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     private bool _isSame;
///     [NotifyPropertyChangeFrom(true ,nameof(ID), nameof(Name), EnableCache = true)]
///     public string IsSame => Name == ID;
///
///     protected void InitializeReactiveObject()
///     {
///         // CacheAtInitialize = true
///        _isSame = Name == ID;
///     }
///
///     protected void RaiseIsSameChange()
///     {
///        this.RaiseAndSetIfChanged(ref _isSame, Name == ID, "IsSame");
///     }
///     private void RaiseAndSetName(ref string backingField, string newValue, bool check = true)
///     {
///         ...
///         RaiseIsSameChange();
///     }
///     private void RaiseAndSetID(ref string backingField, string newValue, bool check = true)
///     {
///         ...
///         RaiseIsSameChange();
///     }
/// }
/// ]]></code></summary>
/// <remarks>
/// 启用 <see cref="CacheMode"/> 非禁用时会生成一个字段来提高性能
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotifyPropertyChangeFromAttribute : Attribute
{
    ///<inheritdoc/>
    /// <param name="PropertyNames">属性名称</param>
    public NotifyPropertyChangeFromAttribute(params string[] PropertyNames)
    {
        this.PropertyNames = PropertyNames;
    }

    ///<inheritdoc/>
    /// <param name="PropertyNames">属性名称</param>
    /// <param name="CacheMode">缓存模式, 非禁用时会生成一个字段来缓存上次目标属性改变后的结果</param>
    public NotifyPropertyChangeFromAttribute(CacheModeEnum CacheMode, params string[] PropertyNames)
    {
        this.PropertyNames = PropertyNames;
        this.CacheMode = CacheMode;
    }

    /// <summary>
    /// 属性名称
    /// </summary>
    public string[] PropertyNames { get; }

    /// <summary>
    /// 缓存模式, 非禁用时会生成一个字段来缓存上次目标属性改变后的结果
    /// </summary>
    public CacheModeEnum CacheMode { get; set; } = CacheModeEnum.Enable;

    /// <summary>
    /// 缓存模式
    /// </summary>
    public enum CacheModeEnum
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
        EnableAfterInitialize,
    }
}
