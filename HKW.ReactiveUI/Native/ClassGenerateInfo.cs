using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI;

internal sealed class ClassGenerateInfo
{
    public ClassGenerateInfo(ClassInfo classInfo)
    {
        Name = classInfo.Name;
        Namespace = classInfo.Namespace;

        HelperPropertyName = Name + "ReactiveHelper";
        HelperObjectName = Name + "ReactiveObjectHelper";
        var reactiveHelperF = new FieldGenerateInfo(
            HelperObjectName,
            "_" + HelperPropertyName.FirstLetterToLower()
        )
        {
            Default = "default!",
        };
        var reactiveHelperP = new PropertyGenerateInfo(
            HelperObjectName,
            HelperPropertyName,
            new($"=> {reactiveHelperF.Name} ?? ({reactiveHelperF.Name} = new(this));")
        )
        {
            Accessibility = Accessibility.Protected,
        };
        Members.Add(reactiveHelperF);
        Members.Add(reactiveHelperP);
    }

    public string HelperPropertyName { get; }

    public string HelperObjectName { get; }
    public string Namespace { get; }
    public string Name { get; }
    public string TypeName => $"{Name}{DeclarationSyntax.TypeParameterList}";

    public string FullName => $"{Namespace}.{Name}";
    public string FullTypeName => $"{Namespace}.{Name}{DeclarationSyntax.TypeParameterList}";
    public SyntaxList<UsingDirectiveSyntax> Usings { get; set; }
    public ClassDeclarationSyntax DeclarationSyntax { get; set; } = null!;

    public List<IMemberGenerateInfo> Members { get; } = [];
    public List<IMemberGenerateInfo> HelperMembers { get; } = [];

    /// <summary>
    /// 所有初始化成员
    /// </summary>
    public List<string> InitializeMembers { get; } = [];

    /// <summary>
    /// (Property, Actions)
    /// </summary>
    public Dictionary<string, List<string>> PropertyChangedMemberByName { get; } = [];

    /// <summary>
    /// (Property, Actions)
    /// </summary>
    public Dictionary<string, List<string>> PropertyChangingMemberByName { get; } = [];
}
