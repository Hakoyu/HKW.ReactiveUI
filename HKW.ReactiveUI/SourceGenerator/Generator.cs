// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

[Generator]
internal partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider.Select(static (c, _) => c.AssemblyName);
        var compilation = context.CompilationProvider.Select(static (c, _) => c);

        var combined = assemblyName.Combine(compilation);

        context.RegisterSourceOutput(
            combined,
            (spc, pair) =>
            {
                var assemblyInfo = new AssemblyInfo(spc, pair.Right);
                foreach (var syntaxTree in pair.Right.SyntaxTrees)
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
                ClassValidator.Execute(
                    assemblyInfo,
                    syntaxTreeInfo,
                    declaredClass,
                    out var classInfo
                )
                is false
            )
                continue;
            if (ClassSourceGenerator.FirstClassFullName == string.Empty)
                ClassSourceGenerator.FirstClassFullName = classInfo.FullTypeName;
            ClassParser.Execute(assemblyInfo, syntaxTreeInfo, declaredClass, classInfo);

            var generateInfo = ClassAnalyzer.Execute(classInfo);
            ClassSourceGenerator.Execute(assemblyInfo, generateInfo);
        }
    }
}

readonly struct AssemblyInfo(SourceProductionContext productionContext, Compilation compilation)
{
    public readonly SourceProductionContext ProductionContext { get; } = productionContext;
    public readonly Compilation Compilation { get; } = compilation;
}

readonly struct SyntaxTreeInfo(SyntaxTree syntaxTree, SemanticModel semanticModel)
{
    public readonly SyntaxTree SyntaxTree { get; } = syntaxTree;
    public readonly SemanticModel SemanticModel { get; } = semanticModel;
}
