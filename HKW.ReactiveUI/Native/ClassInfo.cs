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
    /// (ResourceName, (KeyName, TargetName, ObjectName, RetentionValueOnKeyChange))
    /// </summary>
    public Dictionary<
        string,
        List<(string KeyName, string TargetName, string ObjectName, bool RetentionValueOnKeyChange)>
    > I18nResourceInfoByName { get; } = [];

    /// <summary>
    /// 响应式属性
    /// </summary>
    public List<IPropertySymbol> ReactiveProperties { get; } = [];

    /// <summary>
    /// 可观察属性
    /// </summary>
    public List<IPropertySymbol> ObservableAsProperties { get; } = [];

    ///// <summary>
    ///// (PropertyName, MethodParameterCount)
    ///// </summary>
    //public Dictionary<string, int> OnPropertyChanging { get; set; } = [];

    ///// <summary>
    ///// (PropertyName, MethodParameterCount)
    ///// </summary>
    //public Dictionary<string, int> OnPropertyChanged { get; set; } = [];
}
