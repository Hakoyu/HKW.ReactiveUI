// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal partial class GeneratorExecution
{
    public static GeneratorExecution Load(GeneratorExecutionContext context)
    {
        return new() { ExecutionContext = context };
    }

    public GeneratorExecutionContext ExecutionContext { get; private set; }

    public void Execute()
    {
        foreach (var compilationSyntaxTree in ExecutionContext.Compilation.SyntaxTrees)
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

        ClassParser.Execute(ExecutionContext, semanticModel, declaredClass, classInfo);

        var generateInfo = ClassAnalyzer.Execute(classInfo);
        ClassGenerator.Execute(ExecutionContext, generateInfo);
    }
}
