// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

namespace HKW.HKWReactiveUI;

/// <summary>
/// 名称类型和数据
/// </summary>
internal class NameTypeAndValue(string name, string typeFullName, object value)
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 类型全名
    /// </summary>
    public string TypeFullName { get; } = typeFullName;

    /// <summary>
    /// 数据
    /// </summary>
    public object Value { get; } = value;
}
