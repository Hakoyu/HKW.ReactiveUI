// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ReactivePropertyChangeFromGenerater
{
    public static void Generate(
        AssemblyInfo assemblyInfo,
        ClassInfo classInfo,
        ClassGenerateInfo generateInfo
    )
    {
        var analyzer = new ReactivePropertyChangeFromGenerater()
        {
            _assemblyInfo = assemblyInfo,
            _classInfo = classInfo,
            _generateInfo = generateInfo,
        };
        analyzer.Process();
    }

#pragma warning disable CS8618
    private AssemblyInfo _assemblyInfo;
    private ClassInfo _classInfo;
    private ClassGenerateInfo _generateInfo;
#pragma warning restore CS8618

    private void Process()
    {
        var infos = new List<NotifyPropertyChangeFromInfo>();
        for (var i = 0; i < _classInfo.PropertySymbols.Count; i++)
        {
            var property = _classInfo.PropertySymbols[i];
            var info = ProcessProperty(property.Symbol);
            if (info is not null)
                infos.Add(info);
        }
        Generate(infos);
    }

    private NotifyPropertyChangeFromInfo? ProcessProperty(IPropertySymbol propertySymbol)
    {
        // 获取特性数据
        if (
            propertySymbol.TryGetFirstAttribute(
                typeof(NotifyPropertyChangeFromAttribute).FullName,
                out var attributeData
            )
            is false
        )
            return null;
        // 如果有Set方法则异常
        if (propertySymbol.SetMethod is not null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.PropertyHasSetMethod,
                attributeData.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                    attributeData.ApplicationSyntaxReference.Span
                )
            );
            _assemblyInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        // 获取特性的参数
        var attributeParameters = attributeData.GetParams();
        if (
            attributeParameters.TryGetParams<string>(
                nameof(NotifyPropertyChangeFromAttribute.PropertyNames),
                out var propertyNames
            )
            is false
        )
            return null;

        var getMethod = propertySymbol.GetGetMethodStr();
        if (getMethod is null)
            return null;
        var info = new NotifyPropertyChangeFromInfo(
            propertySymbol.Name,
            propertySymbol.Type,
            getMethod,
            propertyNames.Distinct().ToArray()
        );
        if (
            attributeParameters.TryGetParam<NotifyPropertyChangeFromCacheMode>(
                nameof(NotifyPropertyChangeFromAttribute.CacheMode),
                out var cacheMode
            )
        )
        {
            info.CacheMode = cacheMode;
        }
        return info;
    }

    private void Generate(List<NotifyPropertyChangeFromInfo> infos)
    {
        foreach (var info in infos)
        {
            if (info.CacheMode is NotifyPropertyChangeFromCacheMode.Disable)
                GenerateWhenCacheModeDisable(info);
            else
                GenerateWhenCacheModeEnable(info);
        }
    }

    private void GenerateWhenCacheModeEnable(NotifyPropertyChangeFromInfo info)
    {
        // 缓存字段
        var field = new FieldGenerateInfo($"_{info.PropertyName.FirstLetterToLower()}", info.Type)
        {
            Default = "default!",
        };
        var getMethod = new MethodGenerateInfo(
            $"Get{info.PropertyName}",
            info.Type,
            info.GetMethod
        );
        var raiseMethod = new MethodGenerateInfo(
            $"RaiseAndSet{info.PropertyName}",
            SourceGeneratorHelper.TypeVoid,
            $$"""
            ReactiveUI.IReactiveObjectExtensions.RaiseAndSetIfChanged(this, ref {{field.Name}}, {{getMethod.Name}}(), "{{info.PropertyName}}");
            """
        )
        {
            Accessibility = Accessibility.Protected,
        };
        if (info.CacheMode is NotifyPropertyChangeFromCacheMode.Enable)
            _generateInfo.InitializeMembers.Add($"{field.Name} = {getMethod.Name}();");
        _generateInfo.MemberInfos.Add(field);
        _generateInfo.MemberInfos.Add(getMethod);
        _generateInfo.MemberInfos.Add(raiseMethod);
        foreach (var param in info.Params)
        {
            if (
                _generateInfo.PropertyChangedMemberByName.TryGetValue(param, out var changedMembers)
                is false
            )
                changedMembers = _generateInfo.PropertyChangedMemberByName[param] = [];
            changedMembers.Add($"this.RaisePropertyChanged(\"{info.PropertyName}\");");
        }
    }

    private void GenerateWhenCacheModeDisable(NotifyPropertyChangeFromInfo info)
    {
        foreach (var param in info.Params)
        {
            if (
                _generateInfo.PropertyChangedMemberByName.TryGetValue(param, out var changedMembers)
                is false
            )
                changedMembers = _generateInfo.PropertyChangedMemberByName[param] = [];
            // 如果不启用缓存则直接添加通知
            if (
                _generateInfo.PropertyChangingMemberByName.TryGetValue(
                    param,
                    out var changingMembers
                )
                is false
            )
                changingMembers = _generateInfo.PropertyChangingMemberByName[param] = [];
            changingMembers.Add($"this.RaisePropertyChanging(\"{info.PropertyName}\");");
            changedMembers.Add($"this.RaisePropertyChanged(\"{info.PropertyName}\");");
        }
    }
}
