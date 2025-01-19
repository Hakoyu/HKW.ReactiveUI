using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using HKW.HKWReactiveUI;
using ReactiveUI;
using Splat;

namespace HKW.HKWReactiveUI.Demo;

internal class Program
{
    //private string $Name;
    //public string Name { get; set; } = string.Empty;


    static void Main(string[] args)
    {
        var vm = new TestModel();
        vm.FirstName = "F";
        vm.LastName = "L";
        var f = vm.FullName;
        //LogHostX.AssignLoggerService(typeof(TestModel), LogHost.Default);
        //var p = new ObservablePoint<int>()
        //{
        //    A1 = 1,
        //    A2 = 1,
        //    B1 = 1,
        //    B2 = 1,
        //    C1 = 1,
        //    C2 = 1,
        //    D1 = 1,
        //    D2 = 1
        //};
        //return;
    }

    //private static void TestModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    if (sender is not TestModel model)
    //        return;
    //    Console.WriteLine(
    //        $"{e.PropertyName} = {typeof(TestModel).GetProperty(e.PropertyName!)!.GetValue(sender)}"
    //    );
    //}
}

partial class TestModel : ReactiveObjectX, IEnableLogger<ReactiveObjectX>
{
    public TestModel()
    {
        //var a = this.WhenAnyValue(x => x.B1, x => x.B2)
        //    .Select(x => x)
        //    .ToProperty(this, nameof(Ass));
        //_ass = ;
    }

    //ObservableAsPropertyHelper<int> _ass;
    //[ObservableAsProperty]
    //public int Ass =>
    //    this.WhenAnyValue(x => x.B1).Select(x => x).ToProperty(this, nameof(Ass)).ToDefault<int>();

    [ReactiveProperty(false)]
    public int B1 { get; set; } = int.MaxValue;

    [ReactiveProperty]
    public int B2 { get; set; } = default!;

    public int D1 { get; set; } = int.MaxValue;
    public int D2 { get; set; } = default;

    public TestModel(List<int> list)
    {
        List = list;
    }

    [ReactiveProperty]
    public string FirstName { get; set; } = string.Empty;

    [ReactiveProperty]
    public string LastName { get; set; } = string.Empty;

    [ObservableAsProperty]
    public string FullName =>
        this.WhenAnyValue(x => x.FirstName, x => x.LastName)
            .Select((x, _) => $"{x.Item1} {x.Item2}")
            .ToProperty(this, nameof(FullName))
            .ToDefault<string>();

    [ReactiveProperty]
    public int Number { get; set; } = -1;

    private string _id;

    [ReactiveProperty(false)]
    public string ID { get; set; } = string.Empty;

    [ReactiveProperty]
    public string Name { get; set; } = string.Empty;

    [NotifyPropertyChangeFrom(nameof(Name), nameof(ID))]
    public bool CanExecute => Name == ID;

    [ReactiveProperty]
    public List<int> List { get; set; } = new();

    [NotifyPropertyChangeFrom(nameof(Name))]
    public List<int> List1 => this.To(static x => new List<int>());

    [ReactiveProperty]
    public bool[,] Bools { get; set; }

    /// <summary>
    /// 文化名称
    /// </summary>

    [ReactiveProperty]
    public string CultureName { get; set; } = string.Empty;

    ///// <summary>
    ///// 文化全名
    ///// </summary>
    //[NotifyPropertyChangeFrom(nameof(CultureName))]
    //public string CultureFullName =>
    //    this.To(static x =>
    //    {
    //        if (string.IsNullOrWhiteSpace(x.CultureName))
    //        {
    //            return UnknownCulture;
    //        }
    //        CultureInfo info = null!;
    //        try
    //        {
    //            info = CultureInfo.GetCultureInfo(x.CultureName);
    //        }
    //        catch
    //        {
    //            return UnknownCulture;
    //        }
    //        if (info is not null)
    //        {
    //            return $"{info.DisplayName} [{info.Name}]";
    //        }
    //        return UnknownCulture;
    //    });
    //public static string UnknownCulture => "未知文化";

    //public void OnNameChanging(string value)
    //{
    //    return;
    //}

    /// <summary>
    /// Test
    /// </summary>
    [ReactiveCommand(CanExecute = nameof(CanExecute))]
    public void Test(List<int> list)
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

    public static T ToDefault<T>(this object obj)
    {
        return default!;
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
internal partial class ObservablePoint<T> : ReactiveObjectX, IEquatable<ObservablePoint<T>>
    where T : struct, INumber<T>
{
    /// <inheritdoc/>
    public ObservablePoint() { }

    /// <inheritdoc/>
    /// <param name="x">坐标X</param>
    /// <param name="y">坐标Y</param>
    public ObservablePoint(T x, T y)
    {
        A1 = x;
        A2 = y;
    }

    /// <inheritdoc/>
    [ReactiveProperty]
    public T A1 { get; set; } = T.Zero;

    /// <inheritdoc/>
    [ReactiveProperty]
    public T A2 { get; set; } = default!;

    [ReactiveProperty]
    public int B1 { get; set; } = int.MaxValue;

    [ReactiveProperty]
    public int B2 { get; set; } = default;

    public T C1 { get; set; } = T.Zero;

    public T C2 { get; set; } = default!;

    public int D1 { get; set; } = int.MaxValue;
    public int D2 { get; set; } = default;

    #region Clone
    /// <inheritdoc/>
    public ObservablePoint<T> Clone()
    {
        return new(A1, A2);
    }
    #endregion

    #region Equals

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(A1, A2);
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
        return A1 == other.A1 && A2 == other.A2;
    }
    #endregion
    /// <inheritdoc/>
    public override string ToString()
    {
        return $"X = {A1}, Y = {A2}";
    }
}
