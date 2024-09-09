using ReactiveUI;
using Splat;

namespace HKW.HKWReactiveUI;

/// <inheritdoc cref="ReactiveObject"/>
public partial class ReactiveObjectX : ReactiveObject
{
    /// <inheritdoc/>
    protected ReactiveObjectX()
    {
        InitializeReactiveObject();
    }

    /// <inheritdoc/>
    protected ReactiveObjectX(bool initialize)
    {
        if (initialize)
            InitializeReactiveObject();
    }

    /// <summary>
    /// 初始化 (用于自动生成)
    /// </summary>
    protected virtual void InitializeReactiveObject() { }
}
