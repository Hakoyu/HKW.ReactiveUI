// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

/// <summary>
/// 特性参数值
/// </summary>
internal class AttributeParameterValue
{
    public AttributeParameterValue(TypedConstant typedConstant)
    {
        Value = typedConstant.Value;
    }

    public AttributeParameterValue(IEnumerable<TypedConstant> typedConstants)
    {
        Values = typedConstants.Select(x => x.Value).ToArray();
    }

    public object? Value { get; set; } = null;
    public object?[]? Values { get; set; } = null;
}
