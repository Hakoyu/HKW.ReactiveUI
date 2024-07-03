using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal partial class GeneratorExecution
{
    private void ParseClass(
        SemanticModel semanticModel,
        ClassDeclarationSyntax declaredClass,
        ClassInfo classInfo
    )
    {
        ParseMethod(semanticModel, declaredClass, classInfo);
        ParseProperty(semanticModel, declaredClass, classInfo);
    }

    #region Method
    private void ParseMethod(
        SemanticModel semanticModel,
        ClassDeclarationSyntax declaredClass,
        ClassInfo classInfo
    )
    {
        // 解析方法
        var methodMembers = declaredClass.Members.OfType<MethodDeclarationSyntax>();
        foreach (var methodSyntax in methodMembers)
        {
            var methodSymbol = (IMethodSymbol)
                ModelExtensions.GetDeclaredSymbol(semanticModel, methodSyntax)!;
            ParseReactiveCommand(classInfo, methodSymbol);
        }
    }

    private void ParseReactiveCommand(ClassInfo classInfo, IMethodSymbol methodSymbol)
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
        bool isTask = methodSymbol.ReturnType.AnyBaseTypeIs(
            NativeData.TaskTypeFullName,
            SymbolDisplayFormat.FullyQualifiedFormat
        );
        var realReturnType = isTask
            ? ExecutionContext.Compilation.GetTaskReturnType(methodSymbol.ReturnType)
            : methodSymbol.ReturnType;
        // 是否为空返回值
        var isReturnTypeVoid = ExecutionContext.Compilation.IsVoid(realReturnType);

        classInfo.ReactiveCommandInfos.Add(
            new()
            {
                MethodName = methodSymbol.Name,
                MethodReturnType = isReturnTypeVoid ? null : realReturnType,
                IsTask = isTask,
                ArgumentType = methodSymbol.Parameters.SingleOrDefault()?.Type,
                ReactiveCommandDatas = values
            }
        );
    }

    #endregion

    #region ParseProperty
    private void ParseProperty(
        SemanticModel semanticModel,
        ClassDeclarationSyntax declaredClass,
        ClassInfo classInfo
    )
    {
        // 解析属性
        var propertyMembers = declaredClass.Members.OfType<PropertyDeclarationSyntax>();
        foreach (var propertySyntax in propertyMembers)
        {
            var propertySymbol = (IPropertySymbol)
                ModelExtensions.GetDeclaredSymbol(semanticModel, propertySyntax)!;
            ParseNotifyPropertyChanged(classInfo, propertySymbol);
            ParseI18nProperty(classInfo, propertySymbol);
        }
    }

    #endregion
    private void ParseNotifyPropertyChanged(ClassInfo classInfo, IPropertySymbol propertySymbol)
    {
        // 获取特性数据
        var forAttributeData = propertySymbol
            .GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass!.ToString() == typeof(NotifyPropertyChangedForAttribute).FullName
            );
        // 获取特性的参数
        if (forAttributeData?.TryGetAttributeAndValues(out var forValues) is true)
        {
            // AttributeValue 是目标 PropertySymbol 是源
            if (
                classInfo.NotifyPropertyChanged.TryGetValue(propertySymbol.Name, out var properties)
                is false
            )
                properties = classInfo.NotifyPropertyChanged[propertySymbol.Name] = [];
            if (
                forValues.TryGetValue(
                    nameof(NotifyPropertyChangedForAttribute.PropertyNames),
                    out var value
                )
            )
            {
                if (value.Value?.Value is string propertyName)
                {
                    properties.Add(propertyName);
                }
                else if (value.Values is not null)
                {
                    foreach (var propertyType in value.Values)
                    {
                        if (propertyType.Value is string propertyName1)
                            properties.Add(propertyName1);
                    }
                }
            }
        }

        // 获取特性数据
        var fromAttributeData = propertySymbol
            .GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass!.ToString() == typeof(NotifyPropertyChangedFromAttribute).FullName
            );
        if (fromAttributeData?.TryGetAttributeAndValues(out var fromValues) is true)
        {
            // PropertySymbol 是目标 AttributeValue 是源
            if (
                fromValues.TryGetValue(
                    nameof(NotifyPropertyChangedFromAttribute.PropertyNames),
                    out var value
                )
            )
            {
                if (value.Value?.Value is string propertyName)
                {
                    if (
                        classInfo.NotifyPropertyChanged.TryGetValue(
                            propertyName,
                            out var properties
                        )
                        is false
                    )
                        properties = classInfo.NotifyPropertyChanged[propertyName] = [];
                    properties.Add(propertySymbol.Name);
                }
                else if (value.Values is not null)
                {
                    foreach (var propertyType in value.Values)
                    {
                        if (propertyType.Value is string propertyName1)
                        {
                            if (
                                classInfo.NotifyPropertyChanged.TryGetValue(
                                    propertyName1,
                                    out var properties
                                )
                                is false
                            )
                                properties = classInfo.NotifyPropertyChanged[propertyName1] = [];
                            properties.Add(propertySymbol.Name);
                        }
                    }
                }
            }
        }
    }

    private void ParseI18nProperty(ClassInfo classInfo, IPropertySymbol propertySymbol)
    {
        // 获取特性数据
        var attributeData = propertySymbol
            .GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass!.ToString() == "HKW.HKWUtils.I18nPropertyAttribute"
            );
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
            values.TryGetValue("RetentionValueOnKeyChange", out var retentionValueOnKeyChange);
            if (classInfo.I18nResourceToProperties.TryGetValue(resourceName, out var list) is false)
                list = classInfo.I18nResourceToProperties[resourceName] = [];
            list.Add(
                (keyName, propertySymbol.Name, retentionValueOnKeyChange?.Value?.Value is true)
            );
        }
    }
}
