// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Text;
using System.Text.RegularExpressions;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ClassAnalyzer
{
    public static ClassGenerateInfo Execute(ClassInfo classInfo)
    {
        var t = new ClassAnalyzer() { ClassInfo = classInfo };
        return t.AnalyzeClassInfo();
    }

    public ClassInfo ClassInfo { get; private set; } = null!;
    public ClassGenerateInfo GenerateInfo { get; private set; } = null!;

    private ClassGenerateInfo AnalyzeClassInfo()
    {
        GenerateInfo = new ClassGenerateInfo()
        {
            Name = ClassInfo.Name,
            Namespace = ClassInfo.Namespace,
            DeclarationSyntax = ClassInfo.DeclarationSyntax,
            Usings = ClassInfo.Usings,
            IsReactiveObjectX = ClassInfo.IsReactiveObjectX,
        };

        AnalyzeReactiveProperty();
        AnalyzeReactiveCommand();
        AnalyzeNotifyPropertyChangeFrom();
        AnalyzeI18nObject();

        return GenerateInfo;
    }

    private void AnalyzeReactiveProperty()
    {
        foreach (var property in ClassInfo.ReactiveProperties)
        {
            var typeName = property.Type.ToDisplayString();
            GenerateInfo.ReactivePropertyActionByName.Add(
                property.Name,
                (
                    typeName,
                    $"private void RaiseAndSet{property.Name}(ref {typeName} backingField,{typeName} newValue,bool check = true)"
                )
            );
        }
    }

    private void AnalyzeReactiveCommand()
    {
        foreach (var commandInfo in ClassInfo.ReactiveCommandInfos)
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
                $"private ReactiveUI.ReactiveCommand<{inputType}, {outputType}> "
                    + $"{fieldName} = default!;"
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

            GenerateInfo.Members.Add(sb.ToString());
        }
    }

    private void AnalyzeNotifyPropertyChangeFrom()
    {
        var fields = new HashSet<string>();
        foreach (var pair in ClassInfo.NotifyPropertyChangedFromInfos)
        {
            foreach (var propertyInfo in pair.Value)
            {
                var sb = new StringBuilder();
                var fieldName = $"_{propertyInfo.PropertyName.FirstLetterToLower()}";

                if (propertyInfo.StaticAction)
                {
                    var actionName = $"{fieldName}NotifyPropertyChangeAction";
                    if (fields.Contains(fieldName) is false)
                    {
                        GenerateInfo.Members.Add(
                            $"private static Func<{ClassInfo.FullTypeName},{propertyInfo.Type.ToDisplayString()}> {actionName} = _this => {propertyInfo.Builder};"
                        );
                    }
                    propertyInfo.Builder = new($"{actionName}(this)");
                }

                sb.Append(
                    $"ReactiveUI.IReactiveObjectExtensions.RaiseAndSetIfChanged(this, ref {fieldName}, {propertyInfo.Builder}, nameof({propertyInfo.PropertyName}));"
                );

                if (
                    GenerateInfo.PropertyChangedMemberByName.TryGetValue(pair.Key, out var members)
                    is false
                )
                {
                    members = GenerateInfo.PropertyChangedMemberByName[pair.Key] = [];
                }
                members.Add(sb.ToString());
                if (fields.Add(fieldName))
                {
                    if (propertyInfo.NotifyOnInitialValue)
                    {
                        GenerateInfo.InitializeMembers.Add(sb.ToString());
                    }
                    GenerateInfo.Members.Add(
                        "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
                            + Environment.NewLine
                            + "private "
                            + propertyInfo.Type.ToDisplayString()
                            + " "
                            + fieldName
                            + " = default!;"
                    );
                }
            }
        }
    }

    private void AnalyzeI18nObject()
    {
        var isFirst = true;
        foreach (var i18Info in ClassInfo.I18nResourceInfoByName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{i18Info.Key}.I18nObjects.Add(new(this));");
            if (isFirst)
                sb.AppendLine($"var i18nObject = {i18Info.Key}.I18nObjects.Last();");
            else
                sb.AppendLine($"i18nObject = {i18Info.Key}.I18nObjects.Last();");
            foreach (
                var (keyName, targetName, objectName, retentionValueOnKeyChange) in i18Info.Value
            )
                sb.AppendLine(
                    $"{(string.IsNullOrWhiteSpace(objectName) ? "i18nObject" : objectName)}.AddProperty(nameof({keyName}), x => (({ClassInfo.Name})x).{keyName}, nameof({targetName}), {retentionValueOnKeyChange.ToString().ToLowerInvariant()});"
                );
            isFirst = false;
            GenerateInfo.InitializeMembers.Add(sb.ToString());
        }
    }
}
