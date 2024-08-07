﻿using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal partial class GeneratorExecution
{
    private bool isFirst = true;

    private void GenerateClass(ClassInfo classInfo)
    {
        var stringStream = new StringWriter();
        var writer = new IndentedTextWriter(stringStream, "\t");
        writer.WriteLine("// <auto-generated>");
        // 添加全部引用
        writer.WriteLine(classInfo.Usings);
        // 手动添加ReactiveUI引用
        writer.WriteLine("using ReactiveUI;");
        // 添加命名空间
        writer.WriteLine($"namespace {classInfo.ClassNamespace}");
        writer.WriteLine("{");
        writer.Indent++;
        if (isFirst)
        {
            // 添加ReferenceType特性,并引用ReactiveObject
            // 防止编译器优化,如果整个项目中不引用ReactiveUI,则ReactiveUI的Assembly不会被程序集引用,会导致Fody无法正常构建
            writer.WriteLine("[HKW.HKWReactiveUI.ReferenceType(typeof(ReactiveObject))]");
            isFirst = false;
        }
        // 检测是否为抽象类
        var isAbstract = classInfo.DeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword);
        // 获取可访问性
        var accessibility = classInfo.DeclarationSyntax.Modifiers.GetAccessibility();
        writer.WriteLine(
            $"{accessibility}{(isAbstract ? " abstract" : string.Empty)} partial class {classInfo.ClassFullName}"
        );
        // 添加约束列表
        writer.WriteLine($"{classInfo.DeclarationSyntax.ConstraintClauses}");
        writer.WriteLine("{");
        writer.Indent++;

        GeneratorReactiveCommand(classInfo, writer);
        writer.WriteLine();
        GeneratorInitializeReactiveObject(classInfo, writer);

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

    private void GeneratorReactiveCommand(ClassInfo classInfo, IndentedTextWriter writer)
    {
        foreach (var commandInfo in classInfo.ReactiveCommandInfos)
        {
            var outputType = commandInfo.GetOutputTypeText();
            var inputType = commandInfo.GetInputTypeText();
            var fieldName = $"_{commandInfo.MethodName.FirstLetterToLower()}Command";
            var propretyName = $"{commandInfo.MethodName}Command";
            // 添加DebuggerBrowsable,防止调试器显示
            writer.WriteLine(
                "[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]"
            );
            // 添加ReactiveCommand字段
            writer.WriteLine(
                $"private ReactiveUI.ReactiveCommand<{inputType}, {outputType}> " + $"{fieldName};"
            );
            // 添加属性注释
            writer.WriteLine(
                $"/// <inheritdoc cref=\"{commandInfo.MethodName}({(commandInfo.ArgumentType is null ? string.Empty : inputType)})\"/>"
            );
            // 添加ReactiveCommand属性
            writer.WriteLine(
                $"public ReactiveUI.ReactiveCommand<{inputType}, {outputType}> "
                    + $"{propretyName} => "
            );
            writer.Write($"{fieldName} ?? ({fieldName} = ");
            writer.Write($"ReactiveUI.ReactiveCommand.");
            // 检测异步和参数
            if (commandInfo.ArgumentType is null)
            {
                writer.Write(
                    commandInfo.IsTask is false
                        ? $"Create({commandInfo.MethodName}"
                        : $"CreateFromTask({commandInfo.MethodName}"
                );
            }
            else if (commandInfo.MethodReturnType is not null)
            {
                writer.Write(
                    commandInfo.IsTask is false
                        ? $"Create<{inputType}, {outputType}>({commandInfo.MethodName}"
                        : $"CreateFromTask<{inputType}, {outputType}>({commandInfo.MethodName}"
                );
            }
            else if (commandInfo.MethodReturnType is null)
            {
                writer.Write(
                    commandInfo.IsTask is false
                        ? $"Create<{inputType}>({commandInfo.MethodName}"
                        : $"CreateFromTask<{inputType}>({commandInfo.MethodName}"
                );
            }
            // 如果有CanExecute则添加canExecute参数
            if (
                commandInfo.ReactiveCommandDatas.TryGetValue(
                    nameof(ReactiveCommandAttribute.CanExecute),
                    out var reactiveCommandData
                )
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

    private void GeneratorInitializeReactiveObject(ClassInfo classInfo, IndentedTextWriter writer)
    {
        writer.WriteLine("/// <inheritdoc/>");
        if (classInfo.IsReactiveObjectX)
        {
            writer.WriteLine("protected override void InitializeReactiveObject()");
        }
        else
        {
            writer.WriteLine("protected void InitializeReactiveObject()");
        }
        writer.WriteLine("{");
        writer.Indent++;

        GeneratorNotifyPropertyChanged(classInfo, writer);
        writer.WriteLine();
        GeneratorI18nObject(classInfo, writer);

        writer.Indent--;
        writer.WriteLine("}");
    }

    private void GeneratorNotifyPropertyChanged(ClassInfo classInfo, IndentedTextWriter writer)
    {
        if (classInfo.NotifyPropertyChanged.Count == 0)
            return;
        writer.WriteLine($"PropertyChanged += static (s, e) =>");
        writer.WriteLine("{");
        writer.Indent++;
        writer.WriteLine($"if (s is not {classInfo.ClassFullName} m) return;");
        foreach (var commandExtensionInfo in classInfo.NotifyPropertyChanged)
        {
            writer.WriteLine($"if (e.PropertyName == nameof({commandExtensionInfo.Key}))");
            writer.WriteLine("{");
            writer.Indent++;
            foreach (var propertyName in commandExtensionInfo.Value)
            {
                writer.WriteLine(
                    $"ReactiveUI.IReactiveObjectExtensions.RaisePropertyChanged(m, nameof({propertyName}));"
                );
            }
            writer.Indent--;
            writer.WriteLine("}");
        }
        writer.Indent--;
        writer.WriteLine("};");
    }

    private void GeneratorI18nObject(ClassInfo classInfo, IndentedTextWriter writer)
    {
        var isFirst = true;
        foreach (var i18Info in classInfo.I18nResourceByName)
        {
            writer.WriteLine($"{i18Info.Key}.I18nObjects.Add(new(this));");
            if (isFirst)
                writer.WriteLine($"var i18nObject = {i18Info.Key}.I18nObjects.Last();");
            else
                writer.WriteLine($"i18nObject = {i18Info.Key}.I18nObjects.Last();");
            foreach (var (keyName, targetName, retentionValueOnKeyChange) in i18Info.Value)
                writer.WriteLine(
                    $"i18nObject.AddProperty(nameof({keyName}), x => (({classInfo.ClassName})x).{keyName}, nameof({targetName}), {retentionValueOnKeyChange.ToString().ToLowerInvariant()});"
                );
            writer.WriteLine();
            isFirst = false;
        }
    }
}
