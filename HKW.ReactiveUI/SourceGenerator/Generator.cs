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

            var generateInfo = new ClassGenerateInfo(classInfo)
            {
                DeclarationSyntax = declaredClass,
                Usings = classInfo.Usings,
            };

            ReactivePropertyChangeFromGenerator.Generate(classInfo, generateInfo);
            ReactivePropertyGenerator.Generate(classInfo, generateInfo);
            ReactiveCommandGenerator.Generate(classInfo, generateInfo);
            ObservableAsPropertyGenerator.Generate(classInfo, generateInfo);

            ClassSourceWriter.Execute(generateInfo);
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
        var classNamespace = classSymbol.ContainingNamespace.ToString();
        var typeName = declaredClass.Identifier.ValueText;
        var usings = ((CompilationUnitSyntax)syntaxTreeInfo.SyntaxTree.GetRoot()).Usings;
        var classInfo = new ClassInfo
        {
            Name = typeName,
            Namespace = classNamespace,
            Usings = usings,
            DeclarationSyntax = declaredClass,
        };

        // 如果实现了ReactiveObjectX,则标记
        if (classSymbol.InheritedFrom(TypeFullNames.ReactiveObjectX))
            classInfo.IsReactiveObjectX = true;

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
