using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using HKW.HKWReactiveUI;
using ReactiveUI;

namespace HKW.HKWReactiveUI.Demo;

internal class Program
{
    //private string $Name;
    //public string Name { get; set; } = string.Empty;


    static void Main(string[] args)
    {
        var t = new TestModel();
        //var c = t.TestCommand as ICommand;
        t.PropertyChanged += TestModel_PropertyChanged;
        //TestModel_PropertyChanged(true, new(null));
        //Debug.WriteLine($"{t.CanExecute1} | {c.CanExecute(null)}");
        t.ID = "114";
        //Debug.WriteLine($"{t.CanExecute1} | {c.CanExecute(null)}");
        t.Name = "514";
        //Debug.WriteLine($"{t.CanExecute1} | {c.CanExecute(null)}");
        //t.ID = "514";
        //Debug.WriteLine($"{t.CanExecute1} | {c.CanExecute(null)}");
        //t.Name = "114";
        return;
    }

    private static void TestModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TestModel model)
            return;
        Console.WriteLine(
            $"{e.PropertyName} = {typeof(TestModel).GetProperty(e.PropertyName!)!.GetValue(sender)}"
        );
    }
}

partial class TestModel : ReactiveObject
{
    public TestModel()
    {
        var id = 1;
        //OnPropertyChange(ref id, 2, nameof(ID), true);
        //OnPropertyChange(ref id, 2, nameof(ID), false);
        //CanExecute1 = false;
    }

    //private string _tid = string.Empty;
    //public string TID
    //{
    //    get => _tid;
    //    set => RaiseAndSet(ref _tid, value, nameof(TID), false);
    //}

    [ReactiveProperty(false)]
    public string ID { get; set; } = string.Empty;

    [ReactiveProperty]
    public string Name { get; set; } = string.Empty;

    [NotifyPropertyChangeFrom(nameof(Name), nameof(ID))]
    public bool CanExecute => Name == ID;

    [NotifyPropertyChangeFrom(nameof(Name))]
    public List<int> List => new List<int>();

    [NotifyPropertyChangeFrom(nameof(Name))]
    public List<int> List1 => this.To(static x => new List<int>());

    /// <summary>
    /// 文化名称
    /// </summary>

    [ReactiveProperty]
    public string CultureName { get; set; } = string.Empty;

    /// <summary>
    /// 文化全名
    /// </summary>
    [NotifyPropertyChangeFrom(nameof(CultureName))]
    public string CultureFullName =>
        this.To(static x =>
        {
            if (string.IsNullOrWhiteSpace(x.CultureName))
            {
                return UnknownCulture;
            }
            CultureInfo info = null!;
            try
            {
                info = CultureInfo.GetCultureInfo(x.CultureName);
            }
            catch
            {
                return UnknownCulture;
            }
            if (info is not null)
            {
                return $"{info.DisplayName} [{info.Name}]";
            }
            return UnknownCulture;
        });
    public static string UnknownCulture => "未知文化";

    //public void OnNameChanging(string value)
    //{
    //    return;
    //}

    /// <summary>
    /// Test
    /// </summary>
    public void Test()
    {
        Console.WriteLine(nameof(Test));
    }

    /// <summary>
    /// TestAsync
    /// </summary>
    /// <returns></returns>
    [ReactiveCommand]
    public async Task TestAsync()
    {
        await Task.Delay(1000);
        Console.WriteLine(nameof(TestAsync));
    }
}

internal static class TestExtensions
{
    public static IObservable<(T? Previous, T? Current)> Zip<T>(this IObservable<T> source)
    {
        return source.Scan(
            (Previous: default(T), Current: default(T)),
            (pair, current) => (pair.Current, current)
        );
    }

    public static TTarget To<TSource, TTarget>(this TSource source, Func<TSource, TTarget> func)
    {
        return func(source);
    }
}

/// <summary>
/// 可观察点
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
[DebuggerDisplay("({X}, {Y})")]
public partial class ObservablePoint<T> : ReactiveObjectX, IEquatable<ObservablePoint<T>>
    where T : struct, INumber<T>
{
    /// <inheritdoc/>
    public ObservablePoint() { }

    /// <inheritdoc/>
    /// <param name="x">坐标X</param>
    /// <param name="y">坐标Y</param>
    public ObservablePoint(T x, T y)
    {
        X = x;
        Y = y;
    }

    /// <inheritdoc/>
    [ReactiveProperty]
    public T X { get; set; } = default!;

    /// <inheritdoc/>
    [ReactiveProperty]
    public T Y { get; set; } = default!;

    #region Clone
    /// <inheritdoc/>
    public ObservablePoint<T> Clone()
    {
        return new(X, Y);
    }
    #endregion

    #region Equals

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ObservablePoint<T>);
    }

    /// <inheritdoc/>
    public bool Equals(ObservablePoint<T>? other)
    {
        if (other is null)
            return false;
        return X == other.X && Y == other.Y;
    }
    #endregion
    /// <inheritdoc/>
    public override string ToString()
    {
        return $"X = {X}, Y = {Y}";
    }
}

/// <summary>
/// 枚举命令
/// </summary>
/// <typeparam name="TEnum">枚举类型</typeparam>
public partial class ObservableEnum<TEnum> : ReactiveObjectX
    where TEnum : struct, Enum
{
    /// <inheritdoc/>
    public ObservableEnum() { }

    /// <inheritdoc/>
    /// <param name="value">枚举值</param>
    public ObservableEnum(TEnum value)
    {
        //Value = value;
    }

    /// <summary>
    /// 枚举值
    /// </summary>
    [ReactiveProperty]
    public TEnum Value { get; set; }

    private TEnum _value1 = default!;
    public TEnum Value1
    {
        get => _value1;
        set => RaiseAndSetValue1(ref _value1, value);
    }

    private void RaiseAndSetValue1(ref TEnum backingField, TEnum newValue, bool check = true)
    {
        if (check && EqualityComparer<TEnum>.Default.Equals(backingField, newValue))
            return;

        var oldValue = backingField;
        this.RaisePropertyChanging(nameof(Value1));
        backingField = newValue;
        this.RaisePropertyChanged(nameof(Value1));
    }

    #region IsFlagable
    private static Lazy<bool> _isFlagable =
        new(() => Attribute.IsDefined(typeof(TEnum), typeof(FlagsAttribute)));

    /// <summary>
    /// 是可标记的
    /// </summary>
    public static bool IsFlagable => _isFlagable.Value;
    #endregion
}

/// <summary>
/// 添加标志
/// </summary>
/// <typeparam name="TEnum">枚举类型</typeparam>
/// <param name="value">值</param>
/// <param name="flag">标志</param>
/// <returns>添加标志的值</returns>
public delegate TEnum AddFlag<TEnum>(TEnum value, TEnum flag)
    where TEnum : struct, Enum;

/// <summary>
/// 删除标志
/// </summary>
/// <typeparam name="TEnum">枚举类型</typeparam>
/// <param name="value">值</param>
/// <param name="flag">标志</param>
/// <returns>添加标志的值</returns>
public delegate TEnum RemoveFlag<TEnum>(TEnum value, TEnum flag)
    where TEnum : struct, Enum;
