// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.CodeDom.Compiler;
using System.Reflection;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

[Generator]
internal partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilation = context.CompilationProvider.Select(static (c, _) => c);

        context.RegisterSourceOutput(
            compilation,
            static (spc, compilation) =>
            {
                GeneratorHelper.Initialize(spc, compilation);
                var reactiveUIType = compilation.GetTypeByMetadataName(
                    TypeFullNames.IReactiveObject
                );
                if (reactiveUIType is not null)
                    ReactiveUIVersionInfo.CurrentVersion = reactiveUIType
                        .ContainingAssembly
                        .Identity
                        .Version;

                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    ParseSyntaxTree(syntaxTree);
                }
            }
        );
    }

    private static void ParseSyntaxTree(SyntaxTree syntaxTree)
    {
        var semanticModel = GeneratorHelper.Compilation.GetSemanticModel(syntaxTree);
        var syntaxTreeInfo = new SyntaxTreeInfo(syntaxTree, semanticModel);
        var declaredClasses = syntaxTree
            .GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>();
        foreach (var declaredClass in declaredClasses)
        {
            if (ClassValidator(syntaxTreeInfo, declaredClass) is not ClassInfo classInfo)
                continue;

            ReactivePropertyChangeFromGenerator.Generate(classInfo);
            ReactivePropertyGenerator.Generate(classInfo);
            ReactiveCommandGenerator.Generate(classInfo);
            ObservableAsPropertyGenerator.Generate(classInfo);

            ClassSourceWriter.Execute(classInfo);
        }
    }

    private static ClassInfo? ClassValidator(
        SyntaxTreeInfo syntaxTreeInfo,
        ClassDeclarationSyntax declaredClass
    )
    {
        var classSymbol = (INamedTypeSymbol)
            ModelExtensions.GetDeclaredSymbol(syntaxTreeInfo.SemanticModel, declaredClass)!;
        if (
            classSymbol.AllInterfaces.Any(i => i.ToString() == TypeFullNames.IReactiveObject)
            is false
        )
            return null; // 如果没有实现IReactiveObject接口,则跳过

        // 如果不是分布类型,则触发异常
        if (declaredClass.Modifiers.Any(SyntaxKind.PartialKeyword) is false)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.NotPartialClass,
                classSymbol.Locations[0]
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }

        var classInfo = new ClassInfo(syntaxTreeInfo, declaredClass, classSymbol);

        // 分析所有成员
        foreach (var member in declaredClass.Members)
        {
            if (member is MethodDeclarationSyntax methodSyntax)
            {
                methodSyntax.GetLocation();
                var methodSymbol = (IMethodSymbol)
                    ModelExtensions.GetDeclaredSymbol(syntaxTreeInfo.SemanticModel, methodSyntax)!;
                classInfo.MethodSymbols.Add(new(methodSyntax, methodSymbol));
            }
            else if (member is PropertyDeclarationSyntax propertySyntax)
            {
                var propertySymbol = (IPropertySymbol)
                    ModelExtensions.GetDeclaredSymbol(
                        syntaxTreeInfo.SemanticModel,
                        propertySyntax
                    )!;
                classInfo.PropertySymbols.Add(new(propertySyntax, propertySymbol));
            }
        }
        return classInfo;
    }
}
