// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace HKW.HKWReactiveUI;

/// <summary>
/// 从当前属性通知目标属性改变
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotifyPropertyChangedForAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="propertyNames">属性名称</param>
    public NotifyPropertyChangedForAttribute(params string[] propertyNames)
    {
        PropertyNames.AddRange(propertyNames);
    }

    /// <summary>
    /// 属性名称
    /// </summary>
    public List<string> PropertyNames { get; } = [];
}
