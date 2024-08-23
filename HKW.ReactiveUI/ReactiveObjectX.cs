using ReactiveUI;

namespace HKW.HKWReactiveUI;

/// <inheritdoc cref="ReactiveObject"/>
public partial class ReactiveObjectX : ReactiveObject
{
    /// <inheritdoc/>
    public ReactiveObjectX()
    {
        InitializeBefore();
        InitializeReactiveObject();
        InitializeAfter();
    }

    /// <summary>
    /// 初始化之前
    /// </summary>
    protected virtual void InitializeBefore() { }

    /// <summary>
    /// 初始化 (用于自动生成)
    /// </summary>
    protected virtual void InitializeReactiveObject() { }

    /// <summary>
    /// 初始化之后
    /// </summary>
    protected virtual void InitializeAfter() { }
}
