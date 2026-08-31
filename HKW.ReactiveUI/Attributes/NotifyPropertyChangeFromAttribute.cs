namespace HKW.HKWReactiveUI;

/// <summary>
/// 从目标属性通知当前属性改变
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [NotifyPropertyChangeFrom(NotifyPropertyChangeFromCacheMode.Enable, nameof(ID), nameof(Name)]
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
///     [NotifyPropertyChangeFrom(NotifyPropertyChangeFromCacheMode.Enable ,nameof(ID), nameof(Name))]
///     public string IsSame => Name == ID;
///
///     protected void InitializeReactiveObject()
///     {
///         // NotifyPropertyChangeFromCacheMode.Enable
///        _isSame = Name == ID;
///     }
///
///     protected void RaiseIsSameChange()
///     {
///        this.RaiseAndSetIfChanged(ref _isSame, Name == ID, "IsSame");
///     }
///     private void RaiseAndSetName(ref string backingField, string newValue)
///     {
///         ...
///         RaiseIsSameChange();
///     }
///     private void RaiseAndSetID(ref string backingField, string newValue)
///     {
///         ...
///         RaiseIsSameChange();
///     }
/// }
/// ]]></code></summary>
/// <remarks>
/// 启用 <see cref="CacheMode"/> 启用时会生成一个字段来提高性能
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed partial class NotifyPropertyChangeFromAttribute : Attribute
{
    ///<inheritdoc/>
    /// <param name="PropertyNames">属性名称</param>
    public NotifyPropertyChangeFromAttribute(params string[] PropertyNames)
    {
        this.PropertyNames = PropertyNames;
    }

    ///<inheritdoc/>
    /// <param name="PropertyNames">属性名称</param>
    /// <param name="CacheMode">缓存模式</param>
    public NotifyPropertyChangeFromAttribute(
        NotifyPropertyChangeFromCacheMode CacheMode,
        params string[] PropertyNames
    )
    {
        this.PropertyNames = PropertyNames;
        this.CacheMode = CacheMode;
    }

    /// <summary>
    /// 属性名称
    /// </summary>
    public string[] PropertyNames { get; }

    /// <summary>
    /// 缓存模式
    /// </summary>
    public NotifyPropertyChangeFromCacheMode CacheMode { get; }
}
