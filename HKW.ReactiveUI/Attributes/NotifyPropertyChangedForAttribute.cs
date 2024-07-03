// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace HKW.HKWReactiveUI;

/// <summary>
/// 从当前属性通知目标属性改变
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [ReactiveProperty]
///     [NotifyPropertyChangedFor(nameof(IsEnabled))]
///     public string Name { get; set; } = string.Empty;
///
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
/// <param name="PropertyNames">属性名称</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotifyPropertyChangedForAttribute(params string[] PropertyNames) : Attribute
{
    /// <summary>
    /// 属性名称
    /// </summary>
    public string[] PropertyNames { get; } = PropertyNames;
}
