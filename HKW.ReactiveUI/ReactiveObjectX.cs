using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace HKW.HKWReactiveUI;

/// <inheritdoc cref="ReactiveObject"/>
public partial class ReactiveObjectX : ReactiveObject, IDisposable, IDisposableTracker
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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private List<IDisposable>? _disposableList;

    /// <inheritdoc/>
    public List<IDisposable> DisposableList => _disposableList ??= [];

    #region IDisposable
    /// <summary>
    /// 已处理
    /// </summary>
    protected bool _disposed;

    /// <inheritdoc/>
    ~ReactiveObjectX()
    {
        Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (_disposableList is not null)
            {
                for (var i = 0; i < DisposableList.Count; i++)
                    DisposableList[i].Dispose();
                DisposableList.Clear();
            }
        }
        _disposed = true;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Close()
    {
        Dispose();
    }
    #endregion
}
