// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace HKW.HKWReactiveUI.Fody;

/// <summary>
/// ReactiveUI module weaver.
/// </summary>
/// <seealso cref="BaseModuleWeaver" />
public class ModuleWeaver : BaseModuleWeaver
{
    internal static ModuleWeaverLogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Execute()
    {
        //Debugger.Launch();
        Logger = new ModuleWeaverLogger(this, false);

        if (WeaverHelper.Initialize(ModuleDefinition, Logger) is false)
            return;

        // 筛选所有实现了IReactiveObject接口的类
        var classArray = ModuleDefinition
            .GetAllTypes()
            .Where(x => x.BaseType?.IsAssignableFrom(WeaverHelper.IReactiveObject) is true)
            .ToArray();
        foreach (var classType in classArray)
        {
            ReactivePropertyWeaver.Weave(classType);
            NotifyPropertyChangeFromWeaver.Weave(classType);
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<string> GetAssembliesForScanning()
    {
        return
        [
            "mscorlib",
            "netstandard",
            "System",
            "System.Runtime",
            "ReactiveUI",
            "HKW.ReactiveUI",
        ];
    }
}
