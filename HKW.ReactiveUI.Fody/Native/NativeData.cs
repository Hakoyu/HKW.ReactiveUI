using System.CodeDom.Compiler;
using System.Diagnostics;
using Mono.Cecil;

namespace HKW.HKWReactiveUI.Fody;

internal static class NativeData
{
    public static void Initialize(ModuleDefinition moduleDefinition)
    {
        var generatedCodeConstructor = moduleDefinition.ImportReference(
            typeof(GeneratedCodeAttribute).GetConstructor([typeof(string), typeof(string)])!
        );
        var debuggerBrowsableConstructor = moduleDefinition.ImportReference(
            typeof(DebuggerBrowsableAttribute).GetConstructor([typeof(DebuggerBrowsableState)])!
        );

        GeneratedCodeAttribute = new CustomAttribute(generatedCodeConstructor);
        GeneratedCodeAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(
                moduleDefinition.TypeSystem.String,
                ModuleWeaver.HKWReactiveUI.Name
            )
        );
        GeneratedCodeAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(
                moduleDefinition.TypeSystem.String,
                ModuleWeaver.HKWReactiveUI.Version?.ToString() ?? string.Empty
            )
        );

        DebuggerBrowsableAttribute = new CustomAttribute(debuggerBrowsableConstructor);
        DebuggerBrowsableAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(
                moduleDefinition.ImportReference(typeof(DebuggerBrowsableState)),
                (int)DebuggerBrowsableState.Never
            )
        );
    }

    public static CustomAttribute? GeneratedCodeAttribute { get; private set; }
    public static CustomAttribute? DebuggerBrowsableAttribute { get; private set; }

    public static void AddGeneratedCodeAttribute(FieldDefinition field)
    {
        field.CustomAttributes.Add(GeneratedCodeAttribute);
        field.CustomAttributes.Add(DebuggerBrowsableAttribute);
    }
}
