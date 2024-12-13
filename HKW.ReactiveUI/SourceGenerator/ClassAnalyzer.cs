// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Text;
using System.Text.RegularExpressions;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

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
            var typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            GenerateInfo.ReactivePropertyActionByName.Add(
                property.Name,
                (
                    typeName,
                    CommonData.GeneratedCodeAttribute
                        + Environment.NewLine
                        + $"private void RaiseAndSet{property.Name}(ref {typeName} backingField,{typeName} newValue,bool check = true)"
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
                CommonData.GeneratedCodeAttribute
                    + Environment.NewLine
                    + CommonData.DebuggerBrowsableNever
            );
            // 添加ReactiveCommand字段
            sb.AppendLine(
                $"private ReactiveUI.ReactiveCommand<{inputType}, {outputType}> "
                    + $"{fieldName} = default!;"
            );
            // 添加属性注释
            sb.AppendLine(
                $"/// <inheritdoc cref=\"{commandInfo.MethodName}({(commandInfo.ArgumentType is null ? string.Empty : inputType.ToXMLFormat())})\"/>"
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
                commandInfo.ReactiveCommandAttributeParameters.TryGetValue(
                    nameof(ReactiveCommandAttribute.CanExecute),
                    out var reactiveCommandData
                )
            )
            {
                sb.Append(
                    $", DynamicData.Binding.NotifyPropertyChangedEx.WhenValueChanged(this, static x => x.{reactiveCommandData.Value}, true)"
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
            foreach (var info in pair.Value)
            {
                if (
                    GenerateInfo.PropertyChangedMemberByName.TryGetValue(pair.Key, out var members)
                    is false
                )
                    members = GenerateInfo.PropertyChangedMemberByName[pair.Key] = [];
                // 如果不启用缓存
                if (info.EnableCache is false)
                {
                    if (
                        GenerateInfo.PropertyChangingMemberByName.TryGetValue(
                            pair.Key,
                            out var changingMembers
                        )
                        is false
                    )
                        changingMembers = GenerateInfo.PropertyChangingMemberByName[pair.Key] = [];

                    changingMembers.Add($"this.RaisePropertyChanging(\"{info.PropertyName}\");");
                    members.Add($"this.RaisePropertyChanged(\"{info.PropertyName}\");");
                    continue;
                }

                var propertyActionSB = new StringBuilder();
                var fieldName = $"_{info.PropertyName.FirstLetterToLower()}";
                var raiseMethodName = $"Raise{info.PropertyName}Change";
                // 检测为To静态方法
                if (info.StaticAction)
                {
                    var actionName = $"{fieldName}NotifyPropertyChangeAction";
                    if (fields.Contains(fieldName) is false)
                    {
                        GenerateInfo.Members.Add(
                            $"private static Func<{ClassInfo.FullTypeName},{info.Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            )}> {actionName} = _this => {info.Builder};"
                        );
                    }
                    info.Builder = new($"{actionName}(this)");
                }

                propertyActionSB.Append($"{raiseMethodName}();");
                members.Add(propertyActionSB.ToString());
                // 添加字段
                if (fields.Add(fieldName))
                {
                    if (info.InitializeInInitializeObject)
                    {
                        GenerateInfo.InitializeMembers.Add($"{fieldName} = {info.Builder};");
                    }
                    GenerateInfo.Members.Add(
                        CommonData.GeneratedCodeAttribute
                            + Environment.NewLine
                            + CommonData.DebuggerBrowsableNever
                            + Environment.NewLine
                            + "private "
                            + info.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            + " "
                            + fieldName
                            + " = default!;"
                    );
                    GenerateInfo.Members.Add(
                        CommonData.GeneratedCodeAttribute
                            + Environment.NewLine
                            + @$"protected void {raiseMethodName}()
{{
ReactiveUI.IReactiveObjectExtensions.RaiseAndSetIfChanged(this, ref {fieldName}, {info.Builder}, nameof({info.PropertyName}));
}}"
                    );
                }
            }
        }
    }

    private const string DefaultI18nObjectName = "I18nObject";

    private void AnalyzeI18nObject()
    {
        var objectNames = new HashSet<string>();
        foreach (var i18Info in ClassInfo.I18nResourceInfoByName)
        {
            var sb = new StringBuilder();
            foreach (
                var (keyName, targetName, objectName, retentionValueOnKeyChange) in i18Info.Value
            )
            {
                if (objectNames.Add(objectName))
                {
                    sb.AppendLine(
                        $"{i18Info.Key}?.I18nObjects.Add({(string.IsNullOrWhiteSpace(objectName) ? DefaultI18nObjectName : objectName)});"
                    );
                }
                sb.AppendLine(
                    $"{(string.IsNullOrWhiteSpace(objectName) ? DefaultI18nObjectName : objectName)}.AddProperty(nameof({keyName}), x => (({ClassInfo.Name})x).{keyName}, nameof({targetName}), {retentionValueOnKeyChange.ToString().ToLowerInvariant()});"
                );
            }
            GenerateInfo.InitializeMembers.Add(sb.ToString());
        }
    }
}
