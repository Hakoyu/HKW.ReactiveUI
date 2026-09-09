using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Windows.Input;
using HKW.HKWReactiveUI;
using ReactiveUI;
using ReactiveUI.Builder;
using ReactiveUI.Primitives;

namespace HKW.HKWReactiveUI.Demo;

internal class Program
{
    //private string $Name;
    //public string Name { get; set; } = string.Empty;

    static void Main(string[] args)
    {
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();
        var p = new ObservablePoint<int>();
        p.X = -1;
        Console.WriteLine(p.X);
        p.X = 0;
        Console.WriteLine(p.X);
        p.X = 1;
        Console.WriteLine(p.X);
        //mb.FirstName = "114";
        //mb.LastName = "514";
        //Console.WriteLine(mb.FullName);
        //mb.NameBase = "114514";
        //Console.WriteLine(mb.FirstName);
        //mb.NameBase = "114 514";
        //Console.WriteLine(mb.FirstName);
    }
}

//public partial class TestModelBase : ReactiveObject
//{
//    public TestModelBase() { }

//    [ReactiveProperty]
//    public string NameBase { get; set; } = string.Empty;

//    [ReactiveProperty]
//    public bool CanExecute { get; set; }

//    [NotifyPropertyChangeFrom(NotifyPropertyChangeFromCacheMode.Enable, nameof(NameBase))]
//    public List<int> ListBase => new List<int>();

//    [NotifyPropertyChangeFrom(nameof(ListBase))]
//    public List<int> ListBase1 => new List<int>();

//    [ReactiveProperty]
//    public string FirstName { get; set; } = string.Empty;

//    [ReactiveProperty]
//    public string LastName { get; set; } = string.Empty;

//    [ObservableAsProperty]
//    public string FullName =>
//        this.WhenAnyValue(x => x.FirstName, x => x.LastName)
//            .Select(x => $"{x.Item1} {x.Item2}")
//            .ToProperty(this, nameof(FullName))
//            .Value;

//    [ReactiveCommand]
//    public void Test1()
//    {
//        Console.WriteLine(nameof(Test1));
//    }

//    [ReactiveCommand]
//    public void Test2(string str)
//    {
//        Console.WriteLine(nameof(Test2));
//    }

//    [ReactiveCommand(nameof(CanExecute))]
//    public void Test3(string str)
//    {
//        Console.WriteLine(nameof(Test2));
//    }

//    [ReactiveCommand]
//    public async Task TestAsync1()
//    {
//        await Task.Delay(100);
//        Console.WriteLine(nameof(TestAsync1));
//    }

//    [ReactiveCommand]
//    public async Task TestAsync2()
//    {
//        await Task.Delay(100);
//        Console.WriteLine(nameof(TestAsync2));
//    }
//}

//}
//public partial class TestModel : ReactiveObject
//{
//    public TestModel() { }

//    //[ReactiveProperty]
//    //public string Name { get; set; } = string.Empty;
//    [ReactiveCommand]
//    public void Test1()
//    {
//        Console.WriteLine(nameof(Test1));
//    }
//}

//public class NotifyTest : INotifyPropertyChanged
//{
//    public NotifyTest() { }

//    private string _name = string.Empty;
//    public string Name
//    {
//        get => _name;
//        set
//        {
//            if (_name != value)
//            {
//                _name = value;
//                PropertyChanged?.Invoke(this, new(nameof(Name)));
//            }
//        }
//    }
//    public event PropertyChangedEventHandler? PropertyChanged;
//}

/// <summary>
/// 可观察点
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
[DebuggerDisplay("({X}, {Y})")]
internal partial class ObservablePoint<T> : ReactiveObject
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
    public T X { get; set; }

    /// <inheritdoc/>
    [ReactiveProperty]
    public T Y { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"X = {X}, Y = {Y}";
    }

    partial class ObservablePointReactiveObjectHelper
    {
        partial void OnXChanging(T oldValue, T newValue, ref bool cancel)
        {
            if (newValue > T.Zero)
                cancel = true;
        }
    }
}
