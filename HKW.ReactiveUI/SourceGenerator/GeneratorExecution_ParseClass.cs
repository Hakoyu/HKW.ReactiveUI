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

    #region MethodParse
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
            var index = values.FindIndex(v =>
                v.Name == nameof(ReactiveCommandAttribute.CanExecute)
                && string.IsNullOrWhiteSpace(v.Value.ToString())
            );
            if (index != -1)
                values.RemoveAt(index);
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

        classInfo.CommandExtensionInfos.Add(
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

    #region PropertyParse
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
        }
    }

    private static void ParseNotifyPropertyChanged(
        ClassInfo classInfo,
        IPropertySymbol propertySymbol
    )
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
            foreach (var fromValue in forValues)
            {
                if (fromValue.Value?.Value is string propertyName)
                {
                    properties.Add(propertyName);
                }
                else if (fromValue.Values is not null)
                {
                    foreach (var propertyType in fromValue.Values)
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
            foreach (var fromValue in fromValues)
            {
                if (fromValue.Value?.Value is string propertyName)
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
                else if (fromValue.Values is not null)
                {
                    foreach (var propertyType in fromValue.Values)
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
    #endregion
}
