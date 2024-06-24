using System;

namespace HKW.HKWReactiveUI;

[AttributeUsage(AttributeTargets.Method)]
public class ReactiveCommandAttribute : Attribute
{
    public const string AttributeName = "ReactiveCommand";
    public string CanExecute { get; set; } = string.Empty;
    //public string CanExecutePropertyName { get; set; } = string.Empty;
}
