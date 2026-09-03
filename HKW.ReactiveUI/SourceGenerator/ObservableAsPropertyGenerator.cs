using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ObservableAsPropertyGenerator
{
    public static void Generate(ClassInfo classInfo)
    {
        var analyzer = new ObservableAsPropertyGenerator(classInfo);
        analyzer.Process();
    }

    private readonly ClassInfo _classInfo;

    public ObservableAsPropertyGenerator(ClassInfo classInfo)
    {
        _classInfo = classInfo;
    }

    private void Process()
    {
        for (var i = 0; i < _classInfo.PropertySymbols.Count; i++)
        {
            var property = _classInfo.PropertySymbols[i];
            ProcessProperty(property);
        }
    }

    private void ProcessProperty(PropertySS ss)
    {
        ss.OutData(out var propertySyntax, out var propertySymbol);
        if (propertySymbol.GetFirstAttribute(TypeFullNames.ObservableAsProperty) is null)
            return;
        // 如果有Set方法则异常
        if (propertySymbol.SetMethod is not null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.PropertyHasSetMethod,
                propertySyntax.GetLocation(),
                nameof(TypeFullNames.ObservableAsProperty)
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        if (propertySymbol.TryGetGetMethodContent(out var getMethod) is false)
            return;
        // 如果不是ToProperty方法则取消
        if (getMethod.Contains(".ToProperty(") is false)
            return;
        if (getMethod.EndsWith(".Value;") is false)
            return;
        getMethod = getMethod.Substring(0, getMethod.Length - 7) + ";";
        getMethod = getMethod.Replace("this", "_source");
        var oaphType = $"ObservableAsPropertyHelper<{propertySymbol.Type.GetName()}>";
        var oaphInitializaMethodName = propertySymbol.Name + "OAPHInitializa";
        var field = "_" + propertySymbol.Name.FirstLetterToLower() + "OAPH";

        _classInfo.HelperMembers.Add(
            new FieldGenerateInfo(oaphType, field)
            {
                Default = "default!",
                Accessibility = Accessibility.Public,
            }
        );
        _classInfo.InitializeMembers.Add($"{field} = {oaphInitializaMethodName}();");
        _classInfo.HelperMembers.Add(
            new MethodGenerateInfo(oaphType, oaphInitializaMethodName, getMethod.SplitLine())
        );
    }
}
