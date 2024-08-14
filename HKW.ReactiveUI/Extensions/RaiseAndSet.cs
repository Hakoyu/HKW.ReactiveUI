using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ReactiveUI;

namespace HKW.HKWReactiveUI.Extensions;

/// <summary>
///
/// </summary>
public static class IReactiveObjectExtensions
{
    /// <summary>
    /// 引发并设置
    /// </summary>
    /// <typeparam name="TObj">对象类型</typeparam>
    /// <typeparam name="TRet">结果类型</typeparam>
    /// <param name="reactiveObject">对象</param>
    /// <param name="backingField">字段</param>
    /// <param name="newValue">新值</param>
    /// <param name="propertyName">属性名</param>
    /// <returns>值</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="propertyName"/> 为 <see langword="null"/></exception>
    public static TRet RaiseAndSet<TObj, TRet>(
        this TObj reactiveObject,
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string? propertyName = null
    )
        where TObj : IReactiveObject
    {
        if (propertyName is null)
            throw new ArgumentNullException(nameof(propertyName));

        reactiveObject.RaisePropertyChanged(new(propertyName));
        backingField = newValue;
        reactiveObject.RaisePropertyChanged(new(propertyName));
        return newValue;
    }
}
