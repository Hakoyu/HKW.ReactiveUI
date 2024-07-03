using System.Collections.Generic;
using HKW.HKWReactiveUI.SourceGenerator;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI;

internal sealed class ClassInfo
{
    public string ClassNamespace { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;

    public bool IsReactiveObjectX { get; set; }
    public ClassDeclarationSyntax DeclarationSyntax { get; set; } = null!;
    public List<ReactiveCommandInfo> ReactiveCommandInfos { get; } = [];

    /// <summary>
    /// (SourceProperty, TargetProperties)
    /// </summary>
    public Dictionary<string, HashSet<string>> NotifyPropertyChanged { get; } = [];

    /// <summary>
    /// (ResourceName, I18nObjectInfo)
    /// </summary>
    public Dictionary<
        string,
        List<(string KeyName, string TargetName, bool RetentionValueOnKeyChange)>
    > I18nResourceToProperties { get; } = [];
}
