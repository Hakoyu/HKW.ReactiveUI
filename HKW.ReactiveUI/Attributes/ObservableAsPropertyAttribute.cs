namespace HKW.HKWReactiveUI;

/// <summary>
/// 观察属性
/// <para></para>
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [ReactiveProperty]
///     public string FirstName { get; set; } = string.Empty;
///
///     [ReactiveProperty]
///     public string LastName { get; set; } = string.Empty;
///
///     [ObservableAsProperty]
///     public string FullName =>
///         this.WhenAnyValue(x => x.FirstName, x => x.LastName)
///             .Select((f, l) => $"{f} {l}")
///             .ToProperty(this, nameof(FullName))
///             .ToDefault<string>();
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
///     public string FirstName { get; set; } = string.Empty;
///
///     [ReactiveProperty]
///     public string LastName { get; set; } = string.Empty;
///
///     [ObservableAsProperty]
///     public string FullName => _fullName.Value;
///
///     private ObservableAsPropertyHelper<string> _fullName;
///
///     protected void InitializeReactiveObject()
///     {
///        _fullName = this.WhenAnyValue(x => x.FirstName, x => x.LastName)
///             .Select((f, l) => $"{f} {l}")
///             .ToProperty(this, nameof(FullName));
///     }
/// }
/// ]]></code></summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class ObservableAsPropertyAttribute : Attribute { }
