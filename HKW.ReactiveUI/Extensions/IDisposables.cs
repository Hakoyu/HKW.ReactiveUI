using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ReactiveUI;

namespace HKW.HKWReactiveUI;

/// <summary>
///
/// </summary>
public static class ReactiveObjectExtensions
{
    /// <summary>
    /// 记录处理器至处理列表
    /// </summary>
    /// <param name="disposable">处理器</param>
    /// <param name="obj">对象</param>
    public static void Record(this IDisposable disposable, IDisposables obj)
    {
        obj.Disposables.Add(disposable);
    }

    /// <summary>
    /// 处理全部
    /// </summary>
    /// <param name="disposables">处理列表接口</param>
    /// <param name="clear">清理</param>
    public static void DisposeAll(this IDisposables disposables, bool clear = true)
    {
        for (var i = 0; i < disposables.Disposables.Count; i++)
            disposables.Disposables[i].Dispose();
        if (clear)
            disposables.Disposables.Clear();
    }
}

/// <summary>
/// 处理列表接口
/// </summary>
public interface IDisposables
{
    /// <summary>
    /// 处理列表
    /// </summary>
    public List<IDisposable> Disposables { get; }
}
