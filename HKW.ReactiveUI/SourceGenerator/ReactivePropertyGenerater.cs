using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ReactivePropertyGenerator
{
    public static void Generate(ClassInfo classInfo, ClassGenerateInfo generateInfo)
    {
        var analyzer = new ReactivePropertyGenerator(classInfo, generateInfo);
        analyzer.Process();
    }

    private readonly ClassInfo _classInfo;
    private readonly ClassGenerateInfo _generateInfo;

    public ReactivePropertyGenerator(ClassInfo classInfo, ClassGenerateInfo generateInfo)
    {
        _classInfo = classInfo;
        _generateInfo = generateInfo;
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
        if (propertySymbol.GetFirstAttribute(TypeFullNames.ReactiveProperty) is null)
            return;
        // 如果没有Set方法则异常
        if (propertySymbol.SetMethod is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.PropertyNotHaveSetMethod,
                propertySyntax.GetLocation()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        var typeName = propertySymbol.Type.GetName();

        GeneratePartialMethod(propertySymbol, typeName);
        var contents = GenerateSetMethodContexts(propertySymbol, typeName);

        _generateInfo.MemberInfos.Add(
            new MethodGenerateInfo(
                $"RaiseAndSet{propertySymbol.Name}",
                GeneratorHelper.TypeVoid,
                contents
            )
            {
                Params = new()
                {
                    new(typeName, "backingField") { GenerateType = ParameterGenerateType.Ref },
                    new(typeName, "newValue"),
                },
            }
        );
    }

    private List<string> GenerateSetMethodContexts(IPropertySymbol property, string typeName)
    {
        var contents = new List<string>();
        contents.Add($"if (EqualityComparer<{typeName}>.Default.Equals(backingField, newValue))");
        contents.Add("    return;");
        contents.Add("var oldValue = backingField;");
        contents.Add($"this.RaisePropertyChanging(\"{property.Name}\");");
        contents.Add($"On{property.Name}Changing(oldValue,newValue);");
        if (
            _generateInfo.PropertyChangingMemberByName.TryGetValue(
                property.Name,
                out var changingActions
            )
        )
        {
            contents.Add("");
            foreach (var action in changingActions)
                contents.Add(action);
        }

        contents.Add("");
        contents.Add("backingField = newValue;");
        contents.Add("");

        contents.Add($"this.RaisePropertyChanged(\"{property.Name}\");");
        contents.Add($"On{property.Name}Changed(oldValue,newValue);");

        if (
            _generateInfo.PropertyChangedMemberByName.TryGetValue(
                property.Name,
                out var changedActions
            )
        )
        {
            contents.Add("");
            foreach (var action in changedActions)
                contents.Add(action);
        }

        return contents;
    }

    private void GeneratePartialMethod(IPropertySymbol property, string typeName)
    {
        _generateInfo.MemberInfos.Add(
            new MethodGenerateInfo($"On{property.Name}Changing", GeneratorHelper.TypeVoid, "")
            {
                Params = [new(typeName, "oldValue"), new(typeName, "newValue")],
                GenerateType = MethodGenerateType.Partial,
            }
        );
        _generateInfo.MemberInfos.Add(
            new MethodGenerateInfo($"On{property.Name}Changed", GeneratorHelper.TypeVoid, "")
            {
                Params = [new(typeName, "oldValue"), new(typeName, "newValue")],
                GenerateType = MethodGenerateType.Partial,
            }
        );
    }
}
