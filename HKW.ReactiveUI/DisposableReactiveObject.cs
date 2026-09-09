//using System.Diagnostics;
//using System.Reactive.Disposables;
//using System.Runtime.Serialization;

//namespace HKW.HKWReactiveUI;

///// <summary>
///// 一次性反应对象
///// </summary>
//public partial class DisposableReactiveObject : ReactiveObjectX, IDisposable
//{
//    /// <inheritdoc/>
//    protected DisposableReactiveObject()
//        : base() { }

//    /// <inheritdoc/>
//    /// <param name="initialize">初始化</param>
//    protected DisposableReactiveObject(bool initialize)
//        : base(initialize) { }

//    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
//    private CompositeDisposable? _compositeDisposable;

//    /// <inheritdoc/>
//    [IgnoreDataMember]
//    protected CompositeDisposable CompositeDisposable => _compositeDisposable ??= [];

//    #region IDisposable
//    /// <summary>
//    /// 已处理
//    /// </summary>
//    protected bool _disposed;

//    /// <inheritdoc/>
//    public void Dispose()
//    {
//        Dispose(true);
//        GC.SuppressFinalize(this);
//    }

//    /// <inheritdoc/>
//    protected virtual void Dispose(bool disposing)
//    {
//        if (_disposed)
//            return;

//        if (disposing && _compositeDisposable is not null)
//        {
//            CompositeDisposable.Dispose();
//        }
//        _disposed = true;
//    }
//    #endregion
//}
