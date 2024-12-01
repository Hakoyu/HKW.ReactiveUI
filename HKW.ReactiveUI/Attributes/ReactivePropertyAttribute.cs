// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace HKW.HKWReactiveUI;

/// <summary>
/// 响应式属性
/// <para>
/// 使用IL注入的方式为属性添加通知
/// </para>
/// <para>
/// 示例:
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     [ReactiveProperty(Check = true)]
///     public string Name { get; set; } = string.Empty;
/// }
/// ]]></code>
/// </para>
/// 这样就会生成代码
/// <code><![CDATA[
/// partial class MyViewModel : ReactiveObject
/// {
///     private string $Name;
///
///     [ReactiveProperty(Check = true)]
///     public string Name
///     {
///         get => $Name;
///         set => RaiseAndSetName(ref $Name, value, true);
///     }
///
///     private void RaiseAndSetName(ref string backingField, string newValue, bool check = true)
///	    {
///		    if (!check || !EqualityComparer<string>.Default.Equals(backingField, newValue))
///		    {
///			    string oldValue = backingField;
///             this.RaisePropertyChanging("Name");
///             backingField = newValue;
///			    this.RaisePropertyChanged("Name");
///         }
///	    }
/// }
/// ]]></code></summary>
/// <param name="Check">检查</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ReactivePropertyAttribute(bool Check = true) : Attribute
{
    /// <summary>
    /// 检查
    /// </summary>
    public bool Check { get; } = Check;
}
