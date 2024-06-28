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
        Logger ??= new ModuleWeaverLogger(this, false);

        if (Check(ModuleDefinition) is false)
            return;

        ReactivePropertyWeaver.Execute(ModuleDefinition);

        ObservableAsPropertyWeaver.Execute(ModuleDefinition, FindTypeDefinition);

        ReactiveDependencyPropertyWeaver.Execute(ModuleDefinition);
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
            "HKW.ReactiveUI"
        ];
    }

    internal static AssemblyNameReference ReactiveUI { get; private set; } = null!;
    internal static AssemblyNameReference HKWReactiveUI { get; private set; } = null!;

    internal static TypeDefinition[] IReactiveObjectDerivedClasses { get; private set; } = null!;
    internal static TypeReference IReactiveObject { get; private set; } = null!;
    internal static MethodReference RaiseAndSetIfChangedMethod { get; private set; } = null!;
    internal static TypeReference IReactiveObjectExtensions { get; private set; } = null!;

    private bool Check(ModuleDefinition moduleDefinition)
    {
        Logger.LogInfo(nameof(ReactivePropertyWeaver));
        ReactiveUI = ModuleDefinition
            .AssemblyReferences.Where(x => x.Name == "ReactiveUI")
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();
        if (ReactiveUI is null)
        {
            Logger.LogError(
                "Could not find assembly: ReactiveUI ("
                    + string.Join(", ", ModuleDefinition.AssemblyReferences.Select(x => x.Name))
                    + ")"
            );
            return false;
        }
        Logger.LogInfo($"{ReactiveUI.Name} {ReactiveUI.Version}");

        if (moduleDefinition.Assembly.Name.Name == "HKW.ReactiveUI")
        {
            HKWReactiveUI = moduleDefinition.Assembly.Name;
        }
        else
        {
            HKWReactiveUI = ModuleDefinition
                .AssemblyReferences.Where(x => x.Name == "HKW.ReactiveUI")
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();
            if (HKWReactiveUI is null)
            {
                Logger.LogError(
                    "Could not find assembly: HKW.ReactiveUI ("
                        + string.Join(", ", ModuleDefinition.AssemblyReferences.Select(x => x.Name))
                        + ")"
                );
                return false;
            }
        }
        Logger.LogInfo($"{HKWReactiveUI.Name} {HKWReactiveUI.Version}");

        IReactiveObject = new TypeReference(
            "ReactiveUI",
            "IReactiveObject",
            moduleDefinition,
            ReactiveUI
        );

        var reactiveObjectExtensions =
            new TypeReference(
                "ReactiveUI",
                "IReactiveObjectExtensions",
                moduleDefinition,
                ReactiveUI
            ).Resolve() ?? throw new Exception("reactiveObjectExtensions is null");

        RaiseAndSetIfChangedMethod =
            moduleDefinition.ImportReference(
                reactiveObjectExtensions.Methods.Single(x => x.Name == "RaiseAndSetIfChanged")
            ) ?? throw new Exception("raiseAndSetIfChangedMethod is null");

        IReactiveObjectDerivedClasses = moduleDefinition
            .GetAllTypes()
            .Where(x => x.BaseType is not null && IReactiveObject.IsAssignableFrom(x.BaseType))
            .ToArray();
        return true;
    }
}
