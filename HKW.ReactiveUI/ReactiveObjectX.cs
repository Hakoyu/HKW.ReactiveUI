using ReactiveUI;

namespace HKW.HKWReactiveUI;

/// <inheritdoc cref="ReactiveObject"/>
public partial class ReactiveObjectX : ReactiveObject
{
    /// <inheritdoc/>
    public ReactiveObjectX()
    {
        InitializeCommands();
    }

    /// <summary>
    /// 初始化命令
    /// </summary>
    protected virtual void InitializeCommands() { }
}
