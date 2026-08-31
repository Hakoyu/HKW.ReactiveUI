// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI.SourceGenerator;

internal class ReactivePropertyGenerater
{
    public static void Generate(
        AssemblyInfo assemblyInfo,
        ClassInfo classInfo,
        ClassGenerateInfo generateInfo
    )
    {
        var analyzer = new ReactivePropertyGenerater()
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
        for (var i = 0; i < _classInfo.PropertySymbols.Count; i++)
        {
            var property = _classInfo.PropertySymbols[i];
            ProcessProperty(property.Symbol);
        }
    }

    private void ProcessProperty(IPropertySymbol property)
    {
        if (property.GetFirstAttribute(TypeFullNames.ReactiveProperty) is null)
            return;
        var typeName = property.Type.GetName();

        GeneratePartialMethod(property, typeName);
        var contents = GenerateSetMethodContexts(property, typeName);

        _generateInfo.MemberInfos.Add(
            new MethodGenerateInfo(
                $"RaiseAndSet{property.Name}",
                SourceGeneratorHelper.TypeVoid,
                contents
            )
            {
                Params = new()
                {
                    new("backingField", typeName) { GenerateType = ParameterGenerateType.Ref },
                    new("newValue", typeName),
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
            new MethodGenerateInfo($"On{property.Name}Changing", SourceGeneratorHelper.TypeVoid, "")
            {
                Params = [new("oldValue", typeName), new("newValue", typeName)],
                GenerateType = MethodGenerateType.Partial,
            }
        );
        _generateInfo.MemberInfos.Add(
            new MethodGenerateInfo($"On{property.Name}Changed", SourceGeneratorHelper.TypeVoid, "")
            {
                Params = [new("oldValue", typeName), new("newValue", typeName)],
                GenerateType = MethodGenerateType.Partial,
            }
        );
    }
}
