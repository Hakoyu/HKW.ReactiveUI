using HKW.HKWReactiveUI.SourceGenerator;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

internal static class ReactiveUIVersionInfo
{
    public static Version CurrentVersion { get; set; } = null!;
    private static readonly Version ReactiveUI24Version = new(24, 0, 0);
    public static string UnitTypeName =>
        CurrentVersion < ReactiveUI24Version
            ? "System.Reactive.Unit"
            : "ReactiveUI.Primitives.RxVoid";
}

internal class ReactiveCommandInfo
{
    public static string UnitTypeName => ReactiveUIVersionInfo.UnitTypeName;

    public ReactiveCommandInfo(
        string methodName,
        ITypeSymbol? methodReturnType,
        ITypeSymbol? argumentType,
        bool isTask,
        AttributeParamDictionary attributeParams
    )
    {
        MethodName = methodName;
        MethodReturnType = methodReturnType;
        ArgumentType = argumentType;
        IsTask = isTask;
        AttributeParams = attributeParams;
    }

    public string MethodName { get; set; }
    public ITypeSymbol? MethodReturnType { get; set; }
    public ITypeSymbol? ArgumentType { get; set; }
    public bool IsTask { get; set; }

    /// <summary>
    /// (ParamName, TypeAndValue)
    /// </summary>
    public AttributeParamDictionary AttributeParams { get; set; }

    public string GetOutputTypeText()
    {
        return MethodReturnType is null
            ? UnitTypeName
            : MethodReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public string GetInputTypeText()
    {
        return ArgumentType is null
            ? UnitTypeName
            : ArgumentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
