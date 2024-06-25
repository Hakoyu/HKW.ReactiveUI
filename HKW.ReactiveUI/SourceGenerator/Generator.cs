// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

[Generator]
internal class Generator : ISourceGenerator
{
    public Generator() { }

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        ReactiveCommandGenerator.Execute(context);
    }
}
