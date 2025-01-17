// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ClassChecker
{
    public static bool Execute(
        SyntaxTree compilationSyntaxTree,
        SemanticModel semanticModel,
        ClassDeclarationSyntax declaredClass,
        out ClassInfo classInfo
    )
    {
        classInfo = null!;

        var classSymbol = (INamedTypeSymbol)
            ModelExtensions.GetDeclaredSymbol(semanticModel, declaredClass)!;
        if (
            classSymbol.AllInterfaces.Any(i => i.ToString() == NativeData.IReactiveObjectFullName)
            is false
        )
            return false; // 如果没有实现IReactiveObject接口,则跳过

        // 如果不是分布类型,则触发异常
        if (declaredClass.Modifiers.Any(SyntaxKind.PartialKeyword) is false)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.NotPartialClass,
                classSymbol.Locations[0]
            );
            Generator.ExecutionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        var classNamespace = classSymbol.ContainingNamespace.ToString();
        var typeName = declaredClass.Identifier.ValueText;
        var usings = ((CompilationUnitSyntax)compilationSyntaxTree.GetRoot()).Usings;
        classInfo = new ClassInfo
        {
            Name = typeName,
            Namespace = classNamespace,
            Usings = usings,
            DeclarationSyntax = declaredClass,
        };

        // 如果实现了ReactiveObjectX,则标记
        if (classSymbol.InheritedFrom(NativeData.ReactiveObjectXFullName))
            classInfo.IsReactiveObjectX = true;
        return true;
    }
}
