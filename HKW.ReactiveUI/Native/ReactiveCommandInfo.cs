using HKW.HKWReactiveUI.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

internal class ReactiveCommandInfo
{
    public const string UnitTypeName = "System.Reactive.Unit";

    public string Comment { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public ITypeSymbol? MethodReturnType { get; set; }
    public ITypeSymbol? ArgumentType { get; set; }
    public bool IsTask { get; set; }

    /// <summary>
    /// (ParamName, TypeAndValue)
    /// </summary>
    public Dictionary<
        string,
        AttributeParameterValue
    > ReactiveCommandAttributeParameters { get; set; } = [];

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
