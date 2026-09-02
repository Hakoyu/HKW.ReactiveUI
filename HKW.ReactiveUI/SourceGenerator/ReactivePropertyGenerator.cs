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
                propertySyntax.GetLocation(),
                nameof(TypeFullNames.ReactiveProperty)
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        var typeName = propertySymbol.Type.GetName();

        GeneratePartialMethod(propertySymbol);
        var contents = GenerateSetMethodContexts(propertySymbol);

        var raiseMethod = new MethodGenerateInfo(
            GeneratorHelper.TypeVoid,
            $"RaiseAndSet{propertySymbol.Name}",
            contents
        )
        {
            Accessibility = Accessibility.Public,
            Params = new()
            {
                new(typeName, "backingField") { GenerateType = ParameterGenerateType.Ref },
                new(typeName, "newValue"),
            },
        };
        _generateInfo.HelperMembers.Add(raiseMethod);
    }

    public List<string> GenerateSetMethodContexts(IPropertySymbol property)
    {
        var contents = new List<string>();
        contents.Add(
            $"if (EqualityComparer<{property.Type.GetName()}>.Default.Equals(backingField, newValue))"
        );
        contents.Add("    return;");
        contents.Add("var oldValue = backingField;");
        contents.Add($"_source.RaisePropertyChanging(\"{property.Name}\");");
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

        contents.Add($"_source.RaisePropertyChanged(\"{property.Name}\");");
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

    public void GeneratePartialMethod(IPropertySymbol property)
    {
        var typeName = property.Type.GetName();
        _generateInfo.HelperMembers.Add(
            new MethodGenerateInfo(GeneratorHelper.TypeVoid, $"On{property.Name}Changing", "")
            {
                Params = [new(typeName, "oldValue"), new(typeName, "newValue")],
                GenerateType = MethodGenerateType.Partial,
            }
        );
        _generateInfo.HelperMembers.Add(
            new MethodGenerateInfo(GeneratorHelper.TypeVoid, $"On{property.Name}Changed", "")
            {
                Params = [new(typeName, "oldValue"), new(typeName, "newValue")],
                GenerateType = MethodGenerateType.Partial,
            }
        );
    }
}
