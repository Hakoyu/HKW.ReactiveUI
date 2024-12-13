using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using ReactiveUI;

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
    /// <param name="initialize">初始化,指定是否执行 <see cref="InitializeReactiveObject"/></param>
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
