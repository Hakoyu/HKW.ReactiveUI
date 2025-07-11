﻿using System.CodeDom.Compiler;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ClassGenerator
{
    public static void Execute(AssemblyInfo assemblyInfo, ClassGenerateInfo generateInfo)
    {
        var t = new ClassGenerator() { AssemblyInfo = assemblyInfo, GeneratorInfo = generateInfo };
        t.GenerateClass();
    }

    public static string FirstClassFullName = string.Empty;
    public AssemblyInfo AssemblyInfo { get; private set; }
    public ClassGenerateInfo GeneratorInfo { get; private set; } = null!;
    public IndentedTextWriter Writer { get; private set; } = null!;

    private void GenerateClass()
    {
        var stringStream = new StringWriter();
        Writer = new IndentedTextWriter(stringStream, "\t");

        Writer.WriteLine("// <auto-generated>");
        Writer.WriteLine("#nullable enable");
        Writer.WriteLine("#pragma warning disable CS1591");
        // 添加全部引用
        Writer.WriteLine(GeneratorInfo.Usings);
        // 手动添加ReactiveUI引用
        if (GeneratorInfo.Usings.All(x => x.ToString() != "using ReactiveUI;"))
            Writer.WriteLine("using ReactiveUI;");
        // 添加命名空间
        Writer.WriteLine($"namespace {GeneratorInfo.Namespace}");
        Writer.WriteLine("{");
        Writer.Indent++;
        if (GeneratorInfo.FullTypeName == FirstClassFullName)
        {
            // 添加ReferenceType特性,并引用ReactiveObject
            // 防止编译器优化,如果整个项目中不引用ReactiveUI,则ReactiveUI的Assembly不会被程序集引用,会导致Fody无法正常构建
            Writer.WriteLine("[HKW.HKWReactiveUI.ReferenceType(typeof(ReactiveObject))]");
        }
        // 检测是否为抽象类
        var isAbstract = GeneratorInfo.DeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword);
        // 获取可访问性
        var accessibility = GeneratorInfo.DeclarationSyntax.Modifiers.GetAccessibility();
        Writer.WriteLine(
            $"{accessibility}{(isAbstract ? " abstract" : string.Empty)} partial class {GeneratorInfo.TypeName}"
        );
        // 添加约束列表
        Writer.WriteLine($"{GeneratorInfo.DeclarationSyntax.ConstraintClauses}");
        Writer.WriteLine("{");
        Writer.Indent++;

        GenerateMember();
        Writer.WriteLine();
        GenerateInitializeReactiveObject();
        Writer.WriteLine();
        GenerateReactiveProperty();

        Writer.Indent--;
        Writer.WriteLine("}");
        Writer.Indent--;
        Writer.WriteLine("}");

        AssemblyInfo.ProductionContext.AddSource(
            $"{GeneratorInfo.FullTypeName.Replace('<', '{').Replace('>', '}')}.ReactiveUI.g.cs",
            stringStream.ToString()
        );

        //Console.Out.WriteLine(stringStream);
    }

    private void GenerateReactiveProperty()
    {
        foreach (var propertyInfo in GeneratorInfo.ReactivePropertyActionByName)
        {
            Writer.WriteLine(propertyInfo.Value.Action);
            Writer.WriteLine("{");
            Writer.Indent++;

            Writer.WriteLine(
                @$"if (check && EqualityComparer<{propertyInfo.Value.TypeName}>.Default.Equals(backingField, newValue))
                return;
            var oldValue = backingField;"
            );

            Writer.WriteLine($"this.RaisePropertyChanging(nameof({propertyInfo.Key}));");
            Writer.WriteLine($"On{propertyInfo.Key}Changing(oldValue,newValue);");
            if (
                GeneratorInfo.PropertyChangingMemberByName.TryGetValue(
                    propertyInfo.Key,
                    out var changingActions
                )
            )
            {
                Writer.WriteLine();
                foreach (var action in changingActions)
                {
                    Writer.WriteLine(action);
                }
            }

            Writer.WriteLine();
            Writer.WriteLine("backingField = newValue;");
            Writer.WriteLine();

            Writer.WriteLine($"this.RaisePropertyChanged(nameof({propertyInfo.Key}));");
            Writer.WriteLine($"On{propertyInfo.Key}Changed(oldValue,newValue);");

            if (
                GeneratorInfo.PropertyChangedMemberByName.TryGetValue(
                    propertyInfo.Key,
                    out var changedActions
                )
            )
            {
                Writer.WriteLine();
                foreach (var action in changedActions)
                {
                    Writer.WriteLine(action);
                }
            }

            Writer.Indent--;
            Writer.WriteLine("}");

            Writer.WriteLine(
                $"partial void On{propertyInfo.Key}Changing({propertyInfo.Value.TypeName} oldValue,{propertyInfo.Value.TypeName} newValue);"
            );
            Writer.WriteLine(
                $"partial void On{propertyInfo.Key}Changed({propertyInfo.Value.TypeName} oldValue,{propertyInfo.Value.TypeName} newValue);"
            );
        }
    }

    private void GenerateMember()
    {
        foreach (var member in GeneratorInfo.Members)
        {
            Writer.WriteLine(member);
        }
    }

    private void GenerateInitializeReactiveObject()
    {
        Writer.WriteLine("/// <inheritdoc/>");
        Writer.WriteLine(CommonData.GeneratedCodeAttribute);
        if (GeneratorInfo.IsReactiveObjectX)
        {
            Writer.WriteLine("protected override void InitializeReactiveObject()");
        }
        else
        {
            Writer.WriteLine("protected void InitializeReactiveObject()");
        }
        Writer.WriteLine("{");
        Writer.Indent++;

        GenerateInitializeMember();

        Writer.Indent--;
        Writer.WriteLine("}");
    }

    private void GenerateInitializeMember()
    {
        foreach (var member in GeneratorInfo.InitializeMembers)
        {
            Writer.WriteLine(member);
        }
    }
}
