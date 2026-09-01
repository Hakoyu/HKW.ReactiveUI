using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace HKW.HKWReactiveUI.Fody;

internal class ReactivePropertyWeaver
{
    public static void Weave(TypeDefinition classType)
    {
        ModuleWeaver.Logger.LogInfo(nameof(ReactivePropertyWeaver));
        var w = new ReactivePropertyWeaver(classType);
        w.Weave();
    }

    private readonly TypeDefinition _classType;

    public ReactivePropertyWeaver(TypeDefinition obj)
    {
        _classType = obj;
    }

    public void Weave()
    {
        foreach (var property in _classType.Properties)
        {
            WeaveProperty(property);
        }
    }

    public void WeaveProperty(PropertyDefinition property)
    {
        if (property.IsDefined(WeaverHelper.ReactivePropertyAttribute) is false)
            return;

        // 如果没有SetMethod
        if (property.SetMethod is null)
        {
            ModuleWeaver.Logger.LogError(
                $"Property {property.DeclaringType.FullName}.{property.Name} has no setter, therefore it is not possible for the property to change, and thus should not be marked with [ReactiveProperty]"
            );
            return;
        }

        var raiseAndSetMethodDefinition =
            _classType
                .Resolve()
                .Methods.SingleOrDefault(x => x.Name == $"RaiseAndSet{property.Name}")
            ?? throw new WeaverException(
                $"{_classType.FullName} not exists method RaiseAndSet{property.Name}, please check if you have added the partial keyword to class"
            );

        MethodReference raiseAndSetMethod = null!;
        var isGenericClass = _classType.GenericParameters.Count > 0;
        if (isGenericClass)
        {
            // 如果是泛型类型,需要拼接一个完整的泛型类型
            var genericClassType = _classType.MakeGenericInstanceType([
                .. _classType.GenericParameters,
            ]);
            // 这样会生成Class`1<T>::Method 而不是 Class`1::Method, 后者在IL引用中会出错
            raiseAndSetMethod = raiseAndSetMethodDefinition.Bind(genericClassType);
        }
        else
            raiseAndSetMethod = WeaverHelper.ModuleDefinition.ImportReference(
                raiseAndSetMethodDefinition
            );

        // 生成一个新字段, 命名为 $PropertyName
        var field = new FieldDefinition(
            "$" + property.Name,
            FieldAttributes.Private,
            property.PropertyType
        );
        WeaverHelper.AddGeneratedCodeAttribute(field);
        _classType.Fields.Add(field);

        // 寻找旧字段并删除
        var oldField = (FieldReference)
            property.GetMethod.Body.Instructions.Single(x => x.Operand is FieldReference).Operand;
        var oldFieldDefinition = oldField.Resolve();
        _classType.Fields.Remove(oldFieldDefinition);

        // 查看是否存在自动属性初始化器
        var constructors = _classType.Methods.Where(x => x.IsConstructor);
        foreach (
            var (constructor, fieldAssignment) in from constructor in constructors
            let fieldAssignment = constructor.Body.Instructions.SingleOrDefault(x =>
                Equals(x.Operand, oldFieldDefinition)
                || Equals(x.Operand?.ToString(), oldField.ToString())
            )
            select (constructor, fieldAssignment)
        )
        {
            if (fieldAssignment is null)
                continue;
            //使用新字段初始化器替换自动生成的初始化器
            if (isGenericClass)
            {
                constructor
                    .Body.GetILProcessor()
                    .Replace(
                        fieldAssignment,
                        Instruction.Create(fieldAssignment.OpCode, field.BindDefinition(_classType))
                    );
            }
            else
            {
                constructor
                    .Body.GetILProcessor()
                    .Replace(fieldAssignment, Instruction.Create(OpCodes.Stfld, field));
            }
        }

        // 创建 getter
        property.GetMethod.Body = new MethodBody(property.GetMethod);
        property.GetMethod.Body.Emit(il =>
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this.$PropertyName
            il.Emit(OpCodes.Ldfld, field.BindDefinition(_classType));
            // Return the field value that is lying on the stack
            il.Emit(OpCodes.Ret);
        });

        // 创建 setter
        property.SetMethod.Body = new MethodBody(property.SetMethod);
        property.SetMethod.Body.Emit(il =>
        {
            // this
            il.Emit(OpCodes.Ldarg_0);
            // this
            il.Emit(OpCodes.Ldarg_0);
            // ref field
            il.Emit(OpCodes.Ldflda, field.BindDefinition(_classType));
            // newValue
            il.Emit(OpCodes.Ldarg_1);
            // this.RaiseAndSetProperty(ref field, newValue)
            il.Emit(OpCodes.Call, raiseAndSetMethod);
            // Return out of the function
            il.Emit(OpCodes.Ret);
        });
    }
}
