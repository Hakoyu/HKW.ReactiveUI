using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

internal static class TypeFullNames
{
    public static string ReactiveProperty { get; } = typeof(ReactivePropertyAttribute).FullName;
    public static string ReactiveCommand { get; } = typeof(ReactiveCommandAttribute).FullName;
    public static string IReactiveObject { get; } = "ReactiveUI.IReactiveObject";
    public static string ReactiveObjectX { get; } = "HKW.HKWReactiveUI.ReactiveObjectX";
}
