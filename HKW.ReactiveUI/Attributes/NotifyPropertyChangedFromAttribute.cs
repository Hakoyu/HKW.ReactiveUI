namespace HKW.HKWReactiveUI;

/// <summary>
/// 从目标属性通知当前属性改变
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [ReactiveProperty]
///     public string Name { get; set; } = string.Empty;
///
///     [NotifyPropertyChangedFrom(nameof(Name))]
///     public string IsEnabled => !string.IsNullOrWhiteSpace(Name);
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
///     [NotifyPropertyChangedFrom(nameof(Name))]
///     public string IsEnabled => !string.IsNullOrWhiteSpace(Name);
///
///     protected void InitializeReactiveObject()
///     {
///         PropertyChanged += static (s, e) =>
///         {
///             if (s is not MyViewModel m) return;
///             if (e.PropertyName == nameof(ID))
///             {
///             	ReactiveUI.IReactiveObjectExtensions.RaisePropertyChanged(m, nameof(Name));
///             }
///         };
///     }
/// }
/// ]]></code></summary>
/// <remarks>
/// 如果继承了 <see cref="ReactiveObjectX"/> 则会重写 <see cref="ReactiveObjectX.InitializeReactiveObject"/> 方法,不需要手动运行
/// <para>
/// 否则需要手动运行生成的 <see langword="InitializeReactiveObject"/> 方法
/// </para>
/// </remarks>
/// <param name="PropertyNames">属性名称</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotifyPropertyChangedFromAttribute(params string[] PropertyNames) : Attribute
{
    /// <summary>
    /// 属性名称
    /// </summary>
    public string[] PropertyNames { get; } = PropertyNames;
}
