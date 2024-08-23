// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Text;
using System.Text.RegularExpressions;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ClassAnalyzer
{
    public static ClassGenerateInfo Execute(ClassInfo classInfo)
    {
        var t = new ClassAnalyzer();
        return t.AnalyzeClassInfo(classInfo);
    }

    private ClassGenerateInfo AnalyzeClassInfo(ClassInfo classInfo)
    {
        var generateInfo = new ClassGenerateInfo()
        {
            ClassName = classInfo.ClassName,
            ClassNamespace = classInfo.ClassNamespace,
            DeclarationSyntax = classInfo.DeclarationSyntax,
            Usings = classInfo.Usings,
            IsReactiveObjectX = classInfo.IsReactiveObjectX,
        };

        AnalyzeReactiveCommand(classInfo, generateInfo);
        AnalyzeNotifyPropertyChangedFrom(classInfo, generateInfo);
        AnalyzeI18nObject(classInfo, generateInfo);

        return generateInfo;
    }

    private void AnalyzeReactiveCommand(ClassInfo classInfo, ClassGenerateInfo generateInfo)
    {
        foreach (var commandInfo in classInfo.ReactiveCommandInfos)
        {
            var sb = new StringBuilder();
            var outputType = commandInfo.GetOutputTypeText();
            var inputType = commandInfo.GetInputTypeText();
            var fieldName = $"_{commandInfo.MethodName.FirstLetterToLower()}Command";
            var propretyName = $"{commandInfo.MethodName}Command";
            // 添加DebuggerBrowsable,防止调试器显示
            sb.AppendLine(
                "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
            );
            // 添加ReactiveCommand字段
            sb.AppendLine(
                $"private ReactiveUI.ReactiveCommand<{inputType}, {outputType}> " + $"{fieldName};"
            );
            // 添加属性注释
            sb.AppendLine(
                $"/// <inheritdoc cref=\"{commandInfo.MethodName}({(commandInfo.ArgumentType is null ? string.Empty : inputType)})\"/>"
            );
            // 添加ReactiveCommand属性
            sb.AppendLine(
                $"public ReactiveUI.ReactiveCommand<{inputType}, {outputType}> "
                    + $"{propretyName} => "
            );
            sb.Append($"{fieldName} ?? ({fieldName} = ");
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
                commandInfo.ReactiveCommandDatas.TryGetValue(
                    nameof(ReactiveCommandAttribute.CanExecute),
                    out var reactiveCommandData
                )
            )
            {
                sb.Append(
                    $", DynamicData.Binding.NotifyPropertyChangedEx.WhenValueChanged(this, static x => x.{reactiveCommandData.Value?.Value}, true)"
                );
            }
            sb.AppendLine("));");

            generateInfo.Members.Add(sb.ToString());
        }
    }

    private void AnalyzeNotifyPropertyChangedFrom(
        ClassInfo classInfo,
        ClassGenerateInfo generateInfo
    )
    {
        var fields = new HashSet<string>();
        foreach (var pair in classInfo.NotifyPropertyChangedFromInfos)
        {
            foreach (var propertyInfo in pair.Value)
            {
                var sb = new StringBuilder();
                var fieldName = $"_{propertyInfo.PropertyName.FirstLetterToLower()}";

                if (propertyInfo.IsBodied)
                {
                    sb.AppendLine(
                        $"ReactiveUI.IReactiveObjectExtensions.RaiseAndSetIfChanged(this, ref {fieldName}, {propertyInfo.Builder}, nameof({propertyInfo.PropertyName}));"
                    );
                }
                else
                {
                    throw new NotImplementedException("暂不支持使用代码块构造的get方法");
                    //propertyInfo.Builder.Replace("return ", $"{fieldName} = ");
                    //sb.AppendLine(propertyInfo.Builder.ToString());
                }

                if (
                    generateInfo.PropertyChangedMembers.TryGetValue(pair.Key, out var members)
                    is false
                )
                {
                    members = generateInfo.PropertyChangedMembers[pair.Key] = [];
                }
                members.Add(sb.ToString());
                if (fields.Add(fieldName))
                {
                    generateInfo.Members.Add(
                        "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
                            + Environment.NewLine
                            + "private "
                            + propertyInfo.Type.ToDisplayString()
                            + " "
                            + fieldName
                            + ";"
                    );
                }
            }
        }
    }

    private void AnalyzeI18nObject(ClassInfo classInfo, ClassGenerateInfo generateInfo)
    {
        var isFirst = true;
        foreach (var i18Info in classInfo.I18nResourceByName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{i18Info.Key}.I18nObjects.Add(new(this));");
            if (isFirst)
                sb.AppendLine($"var i18nObject = {i18Info.Key}.I18nObjects.Last();");
            else
                sb.AppendLine($"i18nObject = {i18Info.Key}.I18nObjects.Last();");
            foreach (var (keyName, targetName, retentionValueOnKeyChange) in i18Info.Value)
                sb.AppendLine(
                    $"i18nObject.AddProperty(nameof({keyName}), x => (({classInfo.ClassName})x).{keyName}, nameof({targetName}), {retentionValueOnKeyChange.ToString().ToLowerInvariant()});"
                );
            isFirst = false;
            generateInfo.InitializeMembers.Add(sb.ToString());
        }
    }
}
