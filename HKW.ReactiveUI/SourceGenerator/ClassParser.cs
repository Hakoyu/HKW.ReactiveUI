using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using HKW.SourceGeneratorUtils;
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
        var attributeParameters = attributeData.GetAttributeParameters();

        // 是否为异步方法
        bool isTask = methodSymbol.ReturnType.InheritedFrom(
            CommonData.TaskTypeFullName,
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
                ReactiveCommandAttributeParameters = attributeParameters,
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
            var attributeDataByFullName = new Dictionary<string, AttributeData>();
            foreach (var attribute in propertySymbol.GetAttributes())
            {
                if (
                    attributeDataByFullName.ContainsKey(attribute.AttributeClass!.ToString())
                    is false
                )
                    attributeDataByFullName.Add(attribute.AttributeClass!.ToString(), attribute);
            }
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
        var attributeParameters = attributeData.GetAttributeParameters();
        if (
            attributeParameters.TryGetValue(
                nameof(NotifyPropertyChangeFromAttribute.PropertyNames),
                out var value
            )
            is false
        )
            return;

        var methodBuilder = propertySymbol.GetGetMethodInfo(out var staticAction);
        if (methodBuilder is null)
            return;
        var info = new NotifyPropertyChangeFromInfo(
            propertySymbol.Name,
            propertySymbol.Type,
            methodBuilder,
            true,
            staticAction
        );

        if (
            attributeParameters.TryGetValue(
                nameof(NotifyPropertyChangeFromAttribute.InitializeInInitializeObject),
                out var notifyOnInitialValue
            )
        )
        {
            info.InitializeInInitializeObject = notifyOnInitialValue?.Value is true;
        }

        if (
            attributeParameters.TryGetValue(
                nameof(NotifyPropertyChangeFromAttribute.EnableCache),
                out var enableCacheValue
            )
        )
        {
            info.EnableCache = enableCacheValue?.Value is true;
        }

        if (value.Values is null)
            return;

        foreach (var propertyType in value.Values)
        {
            if (propertyType is not string propertyName1)
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
        var attributeParameters = attributeData.GetAttributeParameters();
        if (attributeParameters.Count == 0)
            return;
        if (attributeParameters.TryGetValue("ResourceName", out var resourceNameType) is false)
            return;
        if (
            resourceNameType?.Value is not string resourceName
            || string.IsNullOrWhiteSpace(resourceName)
        )
            return;
        if (attributeParameters.TryGetValue("KeyPropertyName", out var keyNameType) is false)
            return;
        if (keyNameType?.Value is not string keyName || string.IsNullOrWhiteSpace(keyName))
            return;
        attributeParameters.TryGetValue("ObjectName", out var objectNameType);
        attributeParameters.TryGetValue(
            "RetentionValueOnKeyChange",
            out var retentionValueOnKeyChange
        );
        if (ClassInfo.I18nResourceInfoByName.TryGetValue(resourceName, out var infos) is false)
            infos = ClassInfo.I18nResourceInfoByName[resourceName] = [];
        infos.Add(
            (
                keyName,
                propertySymbol.Name,
                objectNameType?.Value?.ToString() ?? string.Empty,
                retentionValueOnKeyChange?.Value is true
            )
        );
    }
}
