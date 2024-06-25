using System;

namespace HKW.HKWReactiveUI;

/// <summary>
/// 响应式命令
/// </summary>

[AttributeUsage(AttributeTargets.Method)]
public class ReactiveCommandAttribute : Attribute
{
    internal const string AttributeFullName = "HKW.HKWReactiveUI.ReactiveCommandAttribute";

    /// <summary>
    /// 可执行
    /// <para>
    /// 填入目标属性名或方法名
    /// </para>
    /// </summary>
    public string CanExecute { get; set; } = string.Empty;
    //public string CanExecutePropertyName { get; set; } = string.Empty;
}
