using System.Collections.Generic;
using System.Text;
using HKW.HKWReactiveUI.SourceGenerator;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI;

internal sealed class ClassInfo
{
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName => $"{Namespace}.{Name}";
    public string FullTypeName => $"{Namespace}.{Name}{DeclarationSyntax.TypeParameterList}";
    public bool IsReactiveObjectX { get; set; }
    public List<MethodSS> MethodSymbols { get; } = [];
    public List<PropertySS> PropertySymbols { get; } = [];
    public SyntaxList<UsingDirectiveSyntax> Usings { get; set; }
    public ClassDeclarationSyntax DeclarationSyntax { get; set; } = null!;
}
