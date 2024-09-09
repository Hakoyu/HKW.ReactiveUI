using System.CodeDom.Compiler;
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
        var t = new ClassParser() { ExecutionContext = executionContext, };

        t.ParseMethod(semanticModel, declaredClass, classInfo);
        t.ParseProperty(semanticModel, declaredClass, classInfo);
    }

    public GeneratorExecutionContext ExecutionContext { get; private set; }

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
        bool isTask = methodSymbol.ReturnType.InheritedFrom(
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
                ReactiveCommandDatas = values,
                Comment = methodSymbol.GetDocumentationCommentXml() ?? string.Empty
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
            ParseNotifyPropertyChangedFrom(classInfo, propertySymbol);
            ParseI18nProperty(classInfo, propertySymbol);
        }
    }

    #endregion

    private void ParseNotifyPropertyChangedFrom(ClassInfo classInfo, IPropertySymbol propertySymbol)
    {
        if (propertySymbol.GetMethod is null)
            return;
        // 获取特性数据
        var attributeData = propertySymbol
            .GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass!.ToString() == typeof(NotifyPropertyChangeFromAttribute).FullName
            );
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

        var methodSyntax = propertySymbol.GetMethod.DeclaringSyntaxReferences.First().GetSyntax();
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
                classInfo.NotifyPropertyChangedFromInfos.TryGetValue(propertyName1, out var infos)
                is false
            )
            {
                infos = classInfo.NotifyPropertyChangedFromInfos[propertyName1] = [];
            }
            infos.Add(info);
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
            if (classInfo.I18nResourceByName.TryGetValue(resourceName, out var list) is false)
                list = classInfo.I18nResourceByName[resourceName] = [];
            list.Add(
                (keyName, propertySymbol.Name, retentionValueOnKeyChange?.Value?.Value is true)
            );
        }
    }
}
