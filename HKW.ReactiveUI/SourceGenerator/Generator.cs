﻿// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HKW.HKWReactiveUI.SourceGenerator;

[Generator]
internal partial class Generator : ISourceGenerator
{
    public Generator() { }

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        GeneratorExecution.Load(context).Execute();
    }
}
