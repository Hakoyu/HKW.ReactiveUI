// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

/// <summary>
/// 名称类型和数据
/// </summary>
internal class NameTypeAndValue
{
    public NameTypeAndValue(string name, TypedConstant value)
    {
        Name = name;
        Value = value;
    }

    public NameTypeAndValue(string name, ImmutableArray<TypedConstant> values)
    {
        Name = name;
        Values = values;
    }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 数据
    /// </summary>
    public TypedConstant? Value { get; }

    public ImmutableArray<TypedConstant>? Values { get; }
}
