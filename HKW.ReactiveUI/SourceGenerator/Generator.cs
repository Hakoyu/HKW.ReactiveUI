// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

[Generator]
internal partial class Generator : ISourceGenerator
{
    public Generator() { }

    public void Initialize(GeneratorInitializationContext context) { }

    public static GeneratorExecutionContext ExecutionContext { get; private set; }

    public void Execute(GeneratorExecutionContext context)
    {
        ExecutionContext = context;
        foreach (var compilationSyntaxTree in context.Compilation.SyntaxTrees)
        {
            ParseSyntaxTree(compilationSyntaxTree);
        }
    }

    private void ParseSyntaxTree(SyntaxTree compilationSyntaxTree)
    {
        var semanticModel = ExecutionContext.Compilation.GetSemanticModel(compilationSyntaxTree);
        var declaredClasses = compilationSyntaxTree
            .GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>();
        foreach (var declaredClass in declaredClasses)
        {
            ParseClassInfo(compilationSyntaxTree, semanticModel, declaredClass);
        }
    }

    private void ParseClassInfo(
        SyntaxTree compilationSyntaxTree,
        SemanticModel semanticModel,
        ClassDeclarationSyntax declaredClass
    )
    {
        if (
            ClassChecker.Execute(
                compilationSyntaxTree,
                semanticModel,
                declaredClass,
                out var classInfo
            )
            is false
        )
            return;
        if (ClassGenerator.FirstClassFullName == string.Empty)
            ClassGenerator.FirstClassFullName = classInfo.FullTypeName;
        ClassParser.Execute(ExecutionContext, semanticModel, declaredClass, classInfo);

        var generateInfo = ClassAnalyzer.Execute(classInfo);
        ClassGenerator.Execute(ExecutionContext, generateInfo);
    }
}
