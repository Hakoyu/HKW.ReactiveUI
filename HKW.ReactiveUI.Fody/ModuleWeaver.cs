// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using Fody;

namespace HKW.HKWReactiveUI.Fody;

/// <summary>
/// ReactiveUI module weaver.
/// </summary>
/// <seealso cref="BaseModuleWeaver" />
public class ModuleWeaver : BaseModuleWeaver
{
    /// <inheritdoc/>
    public override void Execute()
    {
        //Debugger.Launch();
        var logger = new ModuleWeaverLogger(this, false);
        var propertyWeaver = new ReactivePropertyWeaver
        {
            ModuleDefinition = ModuleDefinition,
            Logger = logger
        };
        propertyWeaver.Execute();

        var observableAsPropertyWeaver = new ObservableAsPropertyWeaver
        {
            ModuleDefinition = ModuleDefinition,
            FindType = FindTypeDefinition,
            Logger = logger
        };
        observableAsPropertyWeaver.Execute();

        var reactiveDependencyWeaver = new ReactiveDependencyPropertyWeaver
        {
            ModuleDefinition = ModuleDefinition,
            Logger = logger
        };
        reactiveDependencyWeaver.Execute();
    }

    /// <inheritdoc/>
    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "netstandard";
        yield return "System";
        yield return "System.Runtime";
        yield return "ReactiveUI";
        yield return "HKW.ReactiveUI";
    }
}
