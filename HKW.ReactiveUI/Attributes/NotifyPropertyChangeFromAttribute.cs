namespace HKW.HKWReactiveUI;

/// <summary>
/// 从目标属性通知当前属性改变
/// <para>会生成一个字段来缓存值以提高性能</para>
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
///         // InitializeInInitializeObject = true
///        _isSame = Name == ID;
///     }
///
///     protected void RaiseIsSameChange()
///        {
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
/// 如果未继承 <see cref="ReactiveObjectX"/> 则 <see cref="InitializeInInitializeObject"/> 将不起作用
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

    /// <inheritdoc/>
    /// <param name="InitializeInInitializeObject">在初始化对象时初始化</param>
    /// <param name="PropertyNames">属性名称</param>
    public NotifyPropertyChangeFromAttribute(
        bool InitializeInInitializeObject = true,
        params string[] PropertyNames
    )
    {
        this.InitializeInInitializeObject = InitializeInInitializeObject;
        this.PropertyNames = PropertyNames;
    }

    /// <summary>
    /// 初始化值时发送通知
    /// </summary>
    public bool InitializeInInitializeObject { get; } = true;

    /// <summary>
    /// 属性名称
    /// </summary>
    public string[] PropertyNames { get; }

    /// <summary>
    /// 启用缓存, 会生成一个字段来缓存上次目标属性改变后的结果
    /// </summary>
    public bool EnableCache { get; set; } = true;
}
