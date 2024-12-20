using System;

namespace HKW.HKWReactiveUI;

/// <summary>
/// 响应式命令
/// <para>
/// 使用源生成器为方法添加响应式命令
/// </para>
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///      [ReactiveCommand]
///      public void Test() { }
///
///      [ReactiveCommand]
///      public async Task TestAsync() { }
/// }
/// ]]></code>
/// </para>
/// 这样就会生成代码
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     private ReactiveCommand<Unit, Unit> _testCommand;
///
///     public ReactiveCommand<Unit, Unit> TestCommand =>
///         _testCommand ?? (_testCommand = ReactiveCommand.Create(Test));
///
///     private ReactiveCommand<Unit, Unit> _testAsyncCommand;
///
///     public ReactiveCommand<Unit, Unit> Test1AsyncCommand =>
///         _testAsyncCommand ?? (_testAsyncCommand = ReactiveCommand.CreateFromTask(TestAsync));
/// }
/// ]]></code></summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ReactiveCommandAttribute : Attribute
{
    /// <inheritdoc/>
    public ReactiveCommandAttribute() { }

    /// <inheritdoc/>
    public ReactiveCommandAttribute(string CanExecute)
    {
        this.CanExecute = CanExecute;
    }

    /// <summary>
    /// 可执行 填入目标属性名或方法名
    /// <para>
    /// 示例:
    /// <code><![CDATA[
    /// partial class MyViewModel : ReactiveObject
    /// {
    ///     [ReactiveProperty]
    ///     public bool CanExecute { get; set; }
    ///
    ///     [ReactiveCommand(CanExecute = CanExecute)]
    ///     public void Test() { }
    /// }
    /// ]]></code>
    /// </para>
    /// 这样就会生成代码
    /// <code><![CDATA[
    /// partial class MyViewModel : ReactiveObject
    /// {
    ///     [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ///     private ReactiveCommand<Unit, Unit> _testCommand;
    ///
    ///     public ReactiveCommand<Unit, Unit> TestCommand =>
    ///         _testCommand ?? (_testCommand = ReactiveCommand.Create(Test, this.WhenValueChanged(static x => x.CanExecute)));
    /// }
    /// ]]></code></summary>
    public string CanExecute { get; set; } = string.Empty;
}
