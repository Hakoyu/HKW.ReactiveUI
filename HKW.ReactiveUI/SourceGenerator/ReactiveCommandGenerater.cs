// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ReactiveCommandGenerator
{
    public static void Generate(ClassInfo classInfo, ClassGenerateInfo generateInfo)
    {
        var g = new ReactiveCommandGenerator(classInfo, generateInfo);
        g.Process();
    }

    private readonly ClassInfo _classInfo;
    private readonly ClassGenerateInfo _generateInfo;

    public ReactiveCommandGenerator(ClassInfo classInfo, ClassGenerateInfo generateInfo)
    {
        _classInfo = classInfo;
        _generateInfo = generateInfo;
    }

    private void Process()
    {
        for (var i = 0; i < _classInfo.MethodSymbols.Count; i++)
        {
            var methodSymbol = _classInfo.MethodSymbols[i];
            ProcessMethod(methodSymbol);
        }
    }

    private void ProcessMethod(MethodSS methodSS)
    {
        methodSS.OutData(out var methodSyntax, out var methodSymbol);

        // 获取特性数据
        if (
            methodSymbol.TryGetFirstAttribute(TypeFullNames.ReactiveCommand, out var attributeData)
            is false
        )
            return;
        // 参数太多, 提示异常
        if (methodSymbol.Parameters.Length > 1)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ReactiveCommandParametersGreaterThan1,
                methodSyntax.GetLocation()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        // 获取特性的参数
        var attributeParams = attributeData.GetParams();

        // 是否为异步方法
        bool isTask = methodSymbol.ReturnType.InheritedFrom(
            GeneratorHelper.TaskTypeFullName,
            SymbolDisplayFormat.FullyQualifiedFormat
        );
        var realReturnType = isTask
            ? methodSymbol.ReturnType.GetTaskReturnType()
            : methodSymbol.ReturnType;
        // 是否为空返回值
        var isReturnTypeVoid = realReturnType.IsVoid();

        GeneratoreactiveCommand(
            new(
                methodSymbol.Name,
                isReturnTypeVoid ? null : realReturnType,
                methodSymbol.Parameters.SingleOrDefault()?.Type,
                isTask,
                attributeParams
            )
        );
    }

    private void GeneratoreactiveCommand(ReactiveCommandInfo commandInfo)
    {
        var outputType = commandInfo.GetOutputTypeText();
        var inputType = commandInfo.GetInputTypeText();
        var fieldName = $"_{commandInfo.MethodName.FirstLetterToLower()}Command";
        var propretyName = $"{commandInfo.MethodName}Command";
        var typeName = $"ReactiveUI.ReactiveCommand<{inputType}, {outputType}>";
        var field = new FieldGenerateInfo(fieldName, typeName) { Default = "default!" };
        var property = new PropertyGenerateInfo(
            propretyName,
            typeName,
            new(
                $"=> {fieldName} ?? ({fieldName} = {GenerateGetMethod(commandInfo, outputType, inputType)}"
            )
        )
        {
            Comment =
                $"/// <inheritdoc cref=\"{commandInfo.MethodName}({(commandInfo.ArgumentType is null ? string.Empty : inputType.ReplaceBraces())})\"/>",
            Accessibility = Accessibility.Public,
        };
        _generateInfo.MemberInfos.Add(field);
        _generateInfo.MemberInfos.Add(property);
    }

    private static string GenerateGetMethod(
        ReactiveCommandInfo commandInfo,
        string outputType,
        string inputType
    )
    {
        var sb = new StringBuilder();
        sb.Append($"ReactiveUI.ReactiveCommand.");
        // 检测异步和参数
        if (commandInfo.ArgumentType is null)
        {
            sb.Append(
                commandInfo.IsTask is false
                    ? $"Create({commandInfo.MethodName}"
                    : $"CreateFromTask({commandInfo.MethodName}"
            );
        }
        else if (commandInfo.MethodReturnType is not null)
        {
            sb.Append(
                commandInfo.IsTask is false
                    ? $"Create<{inputType}, {outputType}>({commandInfo.MethodName}"
                    : $"CreateFromTask<{inputType}, {outputType}>({commandInfo.MethodName}"
            );
        }
        else if (commandInfo.MethodReturnType is null)
        {
            sb.Append(
                commandInfo.IsTask is false
                    ? $"Create<{inputType}>({commandInfo.MethodName}"
                    : $"CreateFromTask<{inputType}>({commandInfo.MethodName}"
            );
        }
        // 如果有CanExecute则添加canExecute参数
        if (
            commandInfo.AttributeParams.TryGetParam<string>(
                nameof(ReactiveCommandAttribute.CanExecute),
                out var canExecutePropertyName
            )
        )
        {
            sb.Append(
                $", DynamicData.Binding.NotifyPropertyChangedEx.WhenValueChanged(this, static x => x.{canExecutePropertyName}, true)"
            );
        }
        sb.AppendLine("));");
        return sb.ToString();
    }
}
