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
    /// 记录一次性对象至一次性对象列表
    /// </summary>
    /// <param name="disposable">一次性对象</param>
    /// <param name="tracker">跟踪器</param>
    public static void Record(this IDisposable disposable, IDisposableTracker tracker)
    {
        tracker.DisposableList.Add(disposable);
    }

    /// <summary>
    /// 处理全部一次性对象
    /// </summary>
    /// <param name="disposableTracker">一次性对象跟踪器</param>
    /// <param name="clear">处理后清空列表</param>
    public static void DisposeAll(this IDisposableTracker disposableTracker, bool clear = true)
    {
        for (var i = 0; i < disposableTracker.DisposableList.Count; i++)
            disposableTracker.DisposableList[i].Dispose();
        if (clear)
            disposableTracker.DisposableList.Clear();
    }
}

/// <summary>
/// 一次性对象跟踪器
/// </summary>
public interface IDisposableTracker
{
    /// <summary>
    /// 一次性对象列表
    /// </summary>
    public List<IDisposable> DisposableList { get; }
}
