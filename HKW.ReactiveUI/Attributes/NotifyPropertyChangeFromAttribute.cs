namespace HKW.HKWReactiveUI;

/// <summary>
/// 从目标属性通知当前属性改变
/// <para>会生成一个字段来缓存值以提高性能</para>
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [ReactiveProperty]
///     public string ID { get; set; } = string.Empty;
///
///     [ReactiveProperty]
///     public string Name { get; set; } = string.Empty;
///
///     [NotifyPropertyChangeFrom(true, nameof(ID), nameof(Name))]
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
///     [ReactiveProperty]
///     public string Name
///     {
///         get => $Name;
///         set => this.RaiseAndSetIfChanged(ref $Name, value);
///     }
///
///     [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
///     private bool _isSame;
///     [NotifyPropertyChangeFrom(true ,nameof(ID), nameof(Name))]
///     public string IsSame => !string.IsNullOrWhiteSpace(Name);
///
///     protected void InitializeReactiveObject()
///     {
///         PropertyChanged += ReactiveObjectPropertyChanged
///
///         // NotifyOnInitialValue = true
///         this.RaiseAndSetIfChanged(ref _isSame, Name == ID, nameof(IsSame));
///     }
///
///     private void ReactiveObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
///     {
///         if (e.PropertyName == nameof(ID))
///         {
///             this.RaiseAndSetIfChanged(ref _isSame, Name == ID, nameof(IsSame));
///         }
///         if (e.PropertyName == nameof(Name))
///         {
///             this.RaiseAndSetIfChanged(ref _isSame, Name == ID, nameof(IsSame));
///         }
///     }
///
/// }
/// ]]></code></summary>
/// <remarks>
/// 如果继承了 <see cref="ReactiveObjectX"/> 则会重写 <see cref="ReactiveObjectX.InitializeReactiveObject"/> 方法,不需要手动运行
/// <para>
/// 否则需要手动运行生成的 <see langword="InitializeReactiveObject"/> 方法
/// </para>
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
    /// <param name="NotifyOnInitialValue">初始化值时发送通知</param>
    /// <param name="PropertyNames">属性名称</param>
    public NotifyPropertyChangeFromAttribute(
        bool NotifyOnInitialValue = true,
        params string[] PropertyNames
    )
    {
        this.NotifyOnInitialValue = NotifyOnInitialValue;
        this.PropertyNames = PropertyNames;
    }

    /// <summary>
    /// 初始化值时发送通知
    /// </summary>
    public bool NotifyOnInitialValue { get; } = true;

    /// <summary>
    /// 属性名称
    /// </summary>
    public string[] PropertyNames { get; }
}
