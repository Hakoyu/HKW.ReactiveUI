// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace HKW.HKWReactiveUI;

/// <summary>
/// 从目标属性通知当前属性改变
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [ReactiveProperty]
///     public string Name { get; set; } = string.Empty;
///
///     [NotifyPropertyChangedFrom(nameof(IsEnabled))]
///     public string IsEnabled => !string.IsNullOrWhiteSpace(Name);
///
///     protected void InitializeReactiveObject() { }
/// }
/// ]]></code>
/// </para>
/// 这样就会生成代码
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     public string Name
///     {
///         get => $Name;
///         set => this.RaiseAndSetIfChanged(ref $Name, value);
///     }
///
///     public string IsEnabled => !string.IsNullOrWhiteSpace(Name);
///
///     protected void InitializeReactiveObject()
///     {
///         this.WhenValueChanged(static x => x.Name).Subscribe(x =>
///         {
///             this.RaisePropertyChanged(nameof(IsEnabled));
///         });
///     }
/// }
/// ]]></code></summary>
/// <remarks>
/// 如果继承了 <see cref="ReactiveObjectX"/> 则会重写 <see cref="ReactiveObjectX.InitializeReactiveObject"/> 方法,不需要手动运行
/// <para>
/// 否则需要手动运行生成的 <see langword="InitializeReactiveObject"/> 方法
/// </para>
/// </remarks>
/// <param name="propertyNames">属性名称</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotifyPropertyChangedFromAttribute(params string[] propertyNames) : Attribute
{
    /// <summary>
    /// 属性名称
    /// </summary>
    public string[] PropertyNames { get; } = propertyNames;
}
