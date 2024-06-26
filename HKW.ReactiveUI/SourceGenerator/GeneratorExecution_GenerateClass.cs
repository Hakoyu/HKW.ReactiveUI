﻿using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal partial class GeneratorExecution
{
    private void GenerateClass(ClassInfo classInfo)
    {
        var stringStream = new StringWriter();
        var writer = new IndentedTextWriter(stringStream, "\t");
        writer.WriteLine("// <auto-generated>");
        writer.WriteLine($"namespace {classInfo.ClassNamespace}");
        writer.WriteLine("{");
        writer.Indent++;
        bool isAbstract = classInfo.DeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword);
        var accessibility = classInfo.DeclarationSyntax.Modifiers.GetAccessibility();
        writer.WriteLine(
            $"{accessibility} {(isAbstract ? SyntaxKind.AbstractKeyword : null)} partial class {classInfo.ClassName}"
        );
        writer.WriteLine($"{classInfo.DeclarationSyntax.TypeParameterList}");
        writer.WriteLine($"{classInfo.DeclarationSyntax.ConstraintClauses}");
        writer.WriteLine("{");
        writer.Indent++;

        GeneratorReactiveCommand(classInfo, writer);
        GeneratorNotifyPropertyChanged(classInfo, writer);

        writer.Indent--;
        writer.WriteLine("}");
        writer.Indent--;
        writer.WriteLine("}");

        ExecutionContext.AddSource(
            $"{classInfo.ClassName}.ReactiveUI.g.cs",
            stringStream.ToString()
        );

        //Console.Out.WriteLine(stringStream);
    }

    private void GeneratorReactiveCommand(ClassInfo classExtensionInfo, IndentedTextWriter writer)
    {
        foreach (var commandExtensionInfo in classExtensionInfo.CommandExtensionInfos)
        {
            var outputType = commandExtensionInfo.GetOutputTypeText();
            var inputType = commandExtensionInfo.GetInputTypeText();
            var fieldName = $"_{commandExtensionInfo.MethodName.FirstLetterToLower()}Command";
            var propretyName = $"{commandExtensionInfo.MethodName}Command";
            // 添加DebuggerBrowsable,防止调试器显示
            writer.WriteLine(
                "[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]"
            );
            // 添加ReactiveCommand字段
            writer.WriteLine(
                $"private ReactiveUI.ReactiveCommand<{inputType}, {outputType}> " + $"{fieldName};"
            );
            // 添加ReactiveCommand属性
            writer.WriteLine(
                $"public ReactiveUI.ReactiveCommand<{inputType}, {outputType}> "
                    + $"{propretyName} => "
            );
            writer.Write($"{fieldName} ?? ({fieldName} = ");
            writer.Write($"ReactiveUI.ReactiveCommand.");
            // 检测异步和参数
            if (commandExtensionInfo.ArgumentType is null)
            {
                writer.Write(
                    commandExtensionInfo.IsTask is false
                        ? $"Create({commandExtensionInfo.MethodName}"
                        : $"CreateFromTask({commandExtensionInfo.MethodName}"
                );
            }
            else if (commandExtensionInfo.MethodReturnType is not null)
            {
                writer.Write(
                    commandExtensionInfo.IsTask is false
                        ? $"Create<{inputType}, {outputType}>({commandExtensionInfo.MethodName}"
                        : $"CreateFromTask<{inputType}, {outputType}>({commandExtensionInfo.MethodName}"
                );
            }
            else if (commandExtensionInfo.MethodReturnType is null)
            {
                writer.Write(
                    commandExtensionInfo.IsTask is false
                        ? $"Create<{inputType}>({commandExtensionInfo.MethodName}"
                        : $"CreateFromTask<{inputType}>({commandExtensionInfo.MethodName}"
                );
            }
            // 如果有CanExecute则添加canExecute参数
            if (
                commandExtensionInfo.ReactiveCommandDatas.FirstOrDefault(d =>
                    d.Name == nameof(ReactiveCommandAttribute.CanExecute)
                )
                is NameTypeAndValue reactiveCommandData
            )
            {
                writer.Write(
                    $", DynamicData.Binding.NotifyPropertyChangedEx.WhenValueChanged(this, static x => x.{reactiveCommandData.Value?.Value}, true)"
                );
            }
            writer.WriteLine("));");
            writer.WriteLine();
        }
    }

    private void GeneratorNotifyPropertyChanged(
        ClassInfo classExtensionInfo,
        IndentedTextWriter writer
    )
    {
        if (classExtensionInfo.IsReactiveObjectX)
        {
            writer.WriteLine("protected override void InitializeReactiveObject()");
        }
        else
        {
            writer.WriteLine("protected void InitializeReactiveObject()");
        }
        writer.WriteLine("{");
        writer.Indent++;
        foreach (var commandExtensionInfo in classExtensionInfo.NotifyPropertyChanged)
        {
            writer.WriteLine(
                $"System.ObservableExtensions.Subscribe(DynamicData.Binding.NotifyPropertyChangedEx.WhenValueChanged(this, static x => x.{commandExtensionInfo.Key})"
            );
            writer.WriteLine(", (x =>");
            writer.WriteLine("{");
            writer.Indent++;
            foreach (var propertyName in commandExtensionInfo.Value)
            {
                writer.WriteLine(
                    $"ReactiveUI.IReactiveObjectExtensions.RaisePropertyChanged(this, nameof({propertyName}));"
                );
            }
            writer.Indent--;
            writer.WriteLine("}));");
        }
        writer.Indent--;
        writer.WriteLine("}");
    }
}
