using System.Runtime.CompilerServices;
using ReactiveUI;
using Splat;

namespace HKW.HKWReactiveUI;

/// <inheritdoc cref="ReactiveObject"/>
public partial class ReactiveObjectX : ReactiveObject
{
    /// <inheritdoc/>
    protected ReactiveObjectX()
    {
        InitializeReactiveObject();
    }

    /// <inheritdoc/>
    /// <param name="initialize">初始化,指定是否执行 <see cref="InitializeReactiveObject"/></param>
    protected ReactiveObjectX(bool initialize)
    {
        if (initialize)
            InitializeReactiveObject();
    }

    /// <summary>
    /// 初始化 (用于自动生成)
    /// </summary>
    protected virtual void InitializeReactiveObject() { }

    /// <summary>
    /// 当属性改变时
    /// </summary>
    /// <param name="backingField">字段</param>
    /// <param name="newValue">新值</param>
    /// <param name="propertyName">属性名称</param>
    /// <param name="check">检查</param>
    protected virtual void RaiseAndSet<T>(
        ref T backingField,
        T newValue,
        [CallerMemberName] string propertyName = null!,
        bool check = true
    )
    {
        if (propertyName is null)
            throw new ArgumentNullException(nameof(propertyName));
        if (check && EqualityComparer<T>.Default.Equals(backingField, newValue))
            return;
        var oldValue = backingField;
        this.RaisePropertyChanging(propertyName);
        backingField = newValue;
        this.RaisePropertyChanged(propertyName);
    }
}
