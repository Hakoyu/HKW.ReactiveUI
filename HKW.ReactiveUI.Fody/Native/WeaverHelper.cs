using System.CodeDom.Compiler;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace HKW.HKWReactiveUI.Fody;

internal static class WeaverHelper
{
    public static bool Initialize(ModuleDefinition moduleDefinition, ModuleWeaverLogger logger)
    {
        ModuleDefinition = moduleDefinition;
        Logger = logger;

        ReactiveUI = moduleDefinition
            .AssemblyReferences.Where(x => x.Name == "ReactiveUI")
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();
        if (ReactiveUI is null)
        {
            //Logger.LogError($"Could not find assembly: ReactiveUI in (\"{moduleDefinition.Name}\")");
            return false;
        }
        var reactiveUIAssembly = moduleDefinition.AssemblyResolver.Resolve(ReactiveUI);

        ReactiveUICore = reactiveUIAssembly
            .MainModule.AssemblyReferences.Where(x => x.Name == "ReactiveUI.Core")
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        var reactiveObjectAssembly = ReactiveUICore is null
            ? reactiveUIAssembly
            : moduleDefinition.AssemblyResolver.Resolve(ReactiveUICore);

        IReactiveObject =
            reactiveObjectAssembly.MainModule.GetType("ReactiveUI.IReactiveObject")
            ?? throw new WeaverException(
                $"Could not find ReactiveUI.IReactiveObject in {reactiveObjectAssembly.Name.Name}."
            );

        Logger.LogInfo($"{ReactiveUI.Name} {ReactiveUI.Version}");

        if (moduleDefinition.Assembly.Name.Name == "HKW.ReactiveUI")
        {
            HKWReactiveUI = moduleDefinition.Assembly.Name;
        }
        else
        {
            HKWReactiveUI = moduleDefinition
                .AssemblyReferences.Where(x => x.Name == "HKW.ReactiveUI")
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();
            if (HKWReactiveUI is null)
            {
                Logger.LogError(
                    "Could not find assembly: HKW.ReactiveUI ("
                        + string.Join(", ", moduleDefinition.AssemblyReferences.Select(x => x.Name))
                        + ")"
                );
            }
        }
        Logger.LogInfo($"{HKWReactiveUI!.Name} {HKWReactiveUI.Version}");

        ReactivePropertyAttribute =
            ModuleDefinition.FindType(
                "HKW.HKWReactiveUI",
                "ReactivePropertyAttribute",
                HKWReactiveUI
            ) ?? throw new WeaverException("ReactivePropertyAttribute is null");

        NotifyPropertyChangeFromAttribute =
            ModuleDefinition.FindType(
                "HKW.HKWReactiveUI",
                "NotifyPropertyChangeFromAttribute",
                HKWReactiveUI
            ) ?? throw new WeaverException("NotifyPropertyChangeFromAttribute is null");

        ObservableAsPropertyAttribute =
            ModuleDefinition.FindType(
                "HKW.HKWReactiveUI",
                "ObservableAsPropertyAttribute",
                HKWReactiveUI
            ) ?? throw new WeaverException("ObservableAsPropertyAttribute is null");

        ObservableAsPropertyHelper = ModuleDefinition.FindType(
            "ReactiveUI",
            "ObservableAsPropertyHelper`1",
            ReactiveUI,
            "T"
        );
        InitializeGeneratedCodeAttribute(ModuleDefinition);
        return true;
    }

    public static void InitializeGeneratedCodeAttribute(ModuleDefinition moduleDefinition)
    {
        var generatedCodeConstructor = moduleDefinition.ImportReference(
            typeof(GeneratedCodeAttribute).GetConstructor([typeof(string), typeof(string)])!
        );
        var debuggerBrowsableConstructor = moduleDefinition.ImportReference(
            typeof(DebuggerBrowsableAttribute).GetConstructor([typeof(DebuggerBrowsableState)])!
        );

        GeneratedCodeAttribute = new CustomAttribute(generatedCodeConstructor);
        GeneratedCodeAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(moduleDefinition.TypeSystem.String, HKWReactiveUI.Name)
        );
        GeneratedCodeAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(
                moduleDefinition.TypeSystem.String,
                HKWReactiveUI.Version?.ToString() ?? string.Empty
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

    private static CustomAttribute? GeneratedCodeAttribute { get; set; }
    private static CustomAttribute? DebuggerBrowsableAttribute { get; set; }

    public static void AddGeneratedCodeAttribute(FieldDefinition field)
    {
        field.CustomAttributes.Add(GeneratedCodeAttribute);
        field.CustomAttributes.Add(DebuggerBrowsableAttribute);
    }

    public static ModuleDefinition ModuleDefinition { get; private set; } = null!;
    public static ModuleWeaverLogger Logger { get; private set; } = null!;
    public static AssemblyNameReference ReactiveUI { get; private set; } = null!;
    public static AssemblyNameReference ReactiveUICore { get; private set; } = null!;
    public static AssemblyNameReference HKWReactiveUI { get; private set; } = null!;
    public static TypeDefinition IReactiveObject { get; private set; } = null!;
    public static TypeReference ReactivePropertyAttribute { get; private set; } = null!;
    public static TypeReference NotifyPropertyChangeFromAttribute { get; private set; } = null!;
    public static TypeReference ObservableAsPropertyAttribute { get; private set; } = null!;
    public static TypeReference ObservableAsPropertyHelper { get; private set; } = null!;
}
