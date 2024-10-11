using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI;

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
    /// 所有成员
    /// </summary>
    public List<string> Members { get; set; } = [];

    /// <summary>
    /// 所有初始化成员
    /// </summary>
    public List<string> InitializeMembers { get; set; } = [];

    /// <summary>
    /// (PropertyName, (PropertyTypeFullName ,PropertyAction))
    /// </summary>
    public Dictionary<
        string,
        (string TypeName, string Action)
    > ReactivePropertyActionByName { get; } = [];

    /// <summary>
    /// (Property, Actions)
    /// </summary>
    public Dictionary<string, List<string>> PropertyChangedMemberByName { get; set; } = [];

    /// <summary>
    /// (Property, Actions)
    /// </summary>
    public Dictionary<string, List<string>> PropertyChangingMemberByName { get; set; } = [];
}
