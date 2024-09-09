using System.Collections.Generic;
using System.Text;
using HKW.HKWReactiveUI.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI;

internal sealed class ClassInfo
{
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string FullName => $"{Namespace}.{Name}";
    public string FullTypeName => $"{Namespace}.{Name}{DeclarationSyntax.TypeParameterList}";
    public SyntaxList<UsingDirectiveSyntax> Usings { get; set; }
    public ClassDeclarationSyntax DeclarationSyntax { get; set; } = null!;
    public bool IsReactiveObjectX { get; set; }
    public List<ReactiveCommandInfo> ReactiveCommandInfos { get; } = [];

    /// <summary>
    /// (SourceProperty, PropertyGetMethodInfos)
    /// </summary>
    public Dictionary<
        string,
        HashSet<NotifyPropertyChangeFromInfo>
    > NotifyPropertyChangedFromInfos { get; } = [];

    /// <summary>
    /// (ResourceName, I18nObjectInfo)
    /// </summary>
    public Dictionary<
        string,
        List<(string KeyName, string TargetName, bool RetentionValueOnKeyChange)>
    > I18nResourceByName { get; } = [];
}

internal sealed class ClassGenerateInfo
{
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TypeName => $"{Name}{DeclarationSyntax.TypeParameterList}";

    public string FullName => $"{Namespace}.{Name}";
    public string FullTypeName => $"{Namespace}.{Name}{DeclarationSyntax.TypeParameterList}";
    public SyntaxList<UsingDirectiveSyntax> Usings { get; set; }
    public ClassDeclarationSyntax DeclarationSyntax { get; set; } = null!;

    public bool IsReactiveObjectX { get; set; }

    /// <summary>
    ///
    /// </summary>
    public List<string> Members { get; set; } = [];

    public List<string> InitializeMembers { get; set; } = [];

    /// <summary>
    /// (Property, Actions)
    /// </summary>
    public Dictionary<string, List<string>> PropertyChangedMembers { get; set; } = [];
}
