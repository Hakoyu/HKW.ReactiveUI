using HKW.HKWReactiveUI.SourceGenerator;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

internal class ReactiveCommandInfo
{
    public const string UnitTypeName = "System.Reactive.Unit";

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
