using ReactiveUI;

namespace HKW.HKWReactiveUI;

/// <inheritdoc cref="ReactiveObject"/>
public partial class ReactiveObjectX : ReactiveObject
{
    /// <inheritdoc/>
    public ReactiveObjectX()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    protected virtual void Initialize() { }
}
