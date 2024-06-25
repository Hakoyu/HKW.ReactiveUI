using System.Collections.Generic;
using HKW.HKWReactiveUI.SourceGenerator;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI;

internal class ClassExtensionInfo
{
    public string ClassNamespace { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public ClassDeclarationSyntax DeclarationSyntax { get; set; } = null!;
    public List<CommandExtensionInfo> CommandExtensionInfos { get; set; } = new();
}
