using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ClassParser
{
    public static void Execute(
        GeneratorExecutionContext executionContext,
        SemanticModel semanticModel,
        ClassDeclarationSyntax declaredClass,
        ClassInfo classInfo
    )
    {
        var t = new ClassParser()
        {
            ExecutionContext = executionContext,
            SemanticModel = semanticModel,
            ClassInfo = classInfo,
            DeclaredClass = declaredClass,
        };
        foreach (var member in declaredClass.Members)
        {
            if (member is MethodDeclarationSyntax methodSyntax)
            {
                var methodSymbol = (IMethodSymbol)
                    ModelExtensions.GetDeclaredSymbol(semanticModel, methodSyntax)!;
                t.MethodSymbols.Add(methodSymbol);
            }
            else if (member is PropertyDeclarationSyntax propertySyntax)
            {
                var propertySymbol = (IPropertySymbol)
                    ModelExtensions.GetDeclaredSymbol(semanticModel, propertySyntax)!;
                t.PropertySymbols.Add(propertySymbol);
            }
        }

        t.ParseMethod();
        t.ParseProperty();
    }

    public GeneratorExecutionContext ExecutionContext { get; private set; }
    public SemanticModel SemanticModel { get; private set; } = null!;
    public ClassInfo ClassInfo { get; private set; } = null!;
    public ClassDeclarationSyntax DeclaredClass { get; private set; } = null!;
    public List<IMethodSymbol> MethodSymbols { get; private set; } = [];
    public List<IPropertySymbol> PropertySymbols { get; private set; } = [];

    #region Method
    // 解析方法
    private void ParseMethod()
    {
        foreach (var methodSymbol in MethodSymbols)
        {
            ParseReactiveCommand(methodSymbol);
        }
    }

    private void ParseReactiveCommand(IMethodSymbol methodSymbol)
    {
        // 参数太多,跳过
        if (methodSymbol.Parameters.Length > 1)
            return;
        // 获取特性数据
        var attributeData = methodSymbol
            .GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass!.ToString() == typeof(ReactiveCommandAttribute).FullName
            );
        if (attributeData is null)
            return;
        // 获取特性的参数
        if (attributeData.TryGetAttributeAndValues(out var values))
        {
            // 删除空的CanExecute
            if (
                values.TryGetValue(nameof(ReactiveCommandAttribute.CanExecute), out var name)
                && string.IsNullOrWhiteSpace(name.Value.ToString())
            )
                values.Remove(nameof(ReactiveCommandAttribute.CanExecute));
        }

        // 是否为异步方法
        bool isTask = methodSymbol.ReturnType.InheritedFrom(
            NativeData.TaskTypeFullName,
            SymbolDisplayFormat.FullyQualifiedFormat
        );
        var realReturnType = isTask
            ? ExecutionContext.Compilation.GetTaskReturnType(methodSymbol.ReturnType)
            : methodSymbol.ReturnType;
        // 是否为空返回值
        var isReturnTypeVoid = ExecutionContext.Compilation.IsVoid(realReturnType);

        ClassInfo.ReactiveCommandInfos.Add(
            new()
            {
                MethodName = methodSymbol.Name,
                MethodReturnType = isReturnTypeVoid ? null : realReturnType,
                IsTask = isTask,
                ArgumentType = methodSymbol.Parameters.SingleOrDefault()?.Type,
                ReactiveCommandDatas = values,
                Comment = methodSymbol.GetDocumentationCommentXml() ?? string.Empty
            }
        );
    }

    #endregion

    #region ParseProperty

    // 解析属性
    private void ParseProperty()
    {
        foreach (var propertySymbol in PropertySymbols)
        {
            var attributeDataByFullName = propertySymbol
                .GetAttributes()
                .ToDictionary(x => x.AttributeClass!.ToString(), x => x);
            ParseReactiveProperty(propertySymbol, attributeDataByFullName);
            ParseNotifyPropertyChangedFrom(propertySymbol, attributeDataByFullName);
            ParseReactiveI18nProperty(propertySymbol, attributeDataByFullName);
        }
    }

    #endregion

    private void ParseReactiveProperty(
        IPropertySymbol propertySymbol,
        IDictionary<string, AttributeData> attributeDataByFullName
    )
    {
        if (
            attributeDataByFullName.ContainsKey(typeof(ReactivePropertyAttribute).FullName) is false
        )
            return;
        ClassInfo.ReactiveProperties.Add(propertySymbol);
    }

    private void ParseNotifyPropertyChangedFrom(
        IPropertySymbol propertySymbol,
        IDictionary<string, AttributeData> attributeDataByFullName
    )
    {
        if (propertySymbol.GetMethod is null)
            return;
        // 获取特性数据
        if (
            attributeDataByFullName.TryGetValue(
                typeof(NotifyPropertyChangeFromAttribute).FullName,
                out var attributeData
            )
            is false
        )
            return;
        // 获取特性的参数
        if (attributeData?.TryGetAttributeAndValues(out var attributeArgs) is not true)
            return;

        if (
            attributeArgs.TryGetValue(
                nameof(NotifyPropertyChangeFromAttribute.PropertyNames),
                out var value
            )
            is false
        )
            return;

        var methodBuilder = propertySymbol.GetGetMethodInfo(out var useSelf);
        if (methodBuilder is null)
            return;
        var info = new NotifyPropertyChangeFromInfo(
            propertySymbol.Name,
            propertySymbol.Type,
            methodBuilder,
            true,
            useSelf
        );

        if (
            attributeArgs.TryGetValue(
                nameof(NotifyPropertyChangeFromAttribute.NotifyOnInitialValue),
                out var notifyOnInitialValue
            )
        )
        {
            info.NotifyOnInitialValue = notifyOnInitialValue.Value?.Value is true;
        }

        if (value.Values is null)
            return;

        foreach (var propertyType in value.Values)
        {
            if (propertyType.Value is not string propertyName1)
                continue;
            if (
                ClassInfo.NotifyPropertyChangedFromInfos.TryGetValue(propertyName1, out var infos)
                is false
            )
            {
                infos = ClassInfo.NotifyPropertyChangedFromInfos[propertyName1] = [];
            }
            infos.Add(info);
        }
    }

    private void ParseReactiveI18nProperty(
        IPropertySymbol propertySymbol,
        IDictionary<string, AttributeData> attributeDataByFullName
    )
    {
        // 获取特性数据
        if (
            attributeDataByFullName.TryGetValue(
                "HKW.HKWUtils.ReactiveI18nPropertyAttribute",
                out var attributeData
            )
            is false
        )
            return;
        if (attributeData?.TryGetAttributeAndValues(out var values) is true)
        {
            if (values.TryGetValue("ResourceName", out var resourceNameType) is false)
                return;
            if (
                resourceNameType.Value?.Value is not string resourceName
                || string.IsNullOrWhiteSpace(resourceName)
            )
                return;
            if (values.TryGetValue("KeyPropertyName", out var keyNameType) is false)
                return;
            if (
                keyNameType.Value?.Value is not string keyName
                || string.IsNullOrWhiteSpace(keyName)
            )
                return;
            values.TryGetValue("ObjectName", out var objectNameType);
            values.TryGetValue("RetentionValueOnKeyChange", out var retentionValueOnKeyChange);
            if (ClassInfo.I18nResourceInfoByName.TryGetValue(resourceName, out var list) is false)
                list = ClassInfo.I18nResourceInfoByName[resourceName] = [];
            list.Add(
                (
                    keyName,
                    propertySymbol.Name,
                    objectNameType?.Value?.Value?.ToString() ?? string.Empty,
                    retentionValueOnKeyChange?.Value?.Value is true
                )
            );
        }
    }
}
