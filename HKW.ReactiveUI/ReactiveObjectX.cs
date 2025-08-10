using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace HKW.HKWReactiveUI;

/// <inheritdoc cref="ReactiveObject"/>
public partial class ReactiveObjectX : ReactiveObject, IDisposable
{
#pragma warning disable S1699 // Constructors should only call non-overridable methods
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
#pragma warning restore S1699 // Constructors should only call non-overridable methods

    /// <summary>
    /// 初始化 (用于自动生成)
    /// </summary>
    protected virtual void InitializeReactiveObject() { }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private CompositeDisposable? _compositeDisposable;

    /// <inheritdoc/>
    [IgnoreDataMember]
    protected CompositeDisposable CompositeDisposable => _compositeDisposable ??= [];

    #region IDisposable
    /// <summary>
    /// 已处理
    /// </summary>
    protected bool _disposed;

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

        if (disposing && _compositeDisposable is not null)
        {
            CompositeDisposable.Dispose();
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
