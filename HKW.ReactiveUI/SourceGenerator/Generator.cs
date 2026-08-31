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
                SourceGeneratorHelper.Initialize(compilation);
                var assemblyInfo = new AssemblyInfo(spc, compilation);
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    ParseSyntaxTree(assemblyInfo, syntaxTree);
                }
            }
        );
    }

    private static void ParseSyntaxTree(AssemblyInfo assemblyInfo, SyntaxTree syntaxTree)
    {
        var semanticModel = assemblyInfo.Compilation.GetSemanticModel(syntaxTree);
        var syntaxTreeInfo = new SyntaxTreeInfo(syntaxTree, semanticModel);
        var declaredClasses = syntaxTree
            .GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>();
        foreach (var declaredClass in declaredClasses)
        {
            if (
                ClassValidator(assemblyInfo, syntaxTreeInfo, declaredClass)
                is not ClassInfo classInfo
            )
                continue;
            //ClassParser.Execute(assemblyInfo, syntaxTreeInfo, declaredClass, classInfo);
            var generateInfo = new ClassGenerateInfo()
            {
                Namespace = classInfo.Namespace,
                Name = classInfo.Name,
                DeclarationSyntax = declaredClass,
                Usings = classInfo.Usings,
            };
            ReactivePropertyChangeFromGenerater.Generate(assemblyInfo, classInfo, generateInfo);
            ReactivePropertyGenerater.Generate(assemblyInfo, classInfo, generateInfo);
            ReactiveCommandGenerater.Generate(assemblyInfo, classInfo, generateInfo);

            if (ClassSourceWriter.FirstClassFullName == string.Empty)
                ClassSourceWriter.FirstClassFullName = classInfo.FullTypeName;
            ClassSourceWriter.Execute(assemblyInfo, generateInfo);
        }
    }

    private static ClassInfo? ClassValidator(
        AssemblyInfo assemblyInfo,
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
            assemblyInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
