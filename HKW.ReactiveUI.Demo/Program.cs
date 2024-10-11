using System.ComponentModel;
using System.Diagnostics;
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

    //public void OnNameChanging(string value)
    //{
    //    return;
    //}

    private void OnNameChanging(string oldValue, string newValue)
    {
        return;
    }

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
