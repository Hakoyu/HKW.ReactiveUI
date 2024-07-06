using System.ComponentModel;
using System.Diagnostics;
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
        var c = t.TestCommand as ICommand;
        t.PropertyChanged += TestModel_PropertyChanged;
        Debug.WriteLine($"{t.CanExecute} | {c.CanExecute(null)}");
        t.ID = "114";
        Debug.WriteLine($"{t.CanExecute} | {c.CanExecute(null)}");
        t.Name = "514";
        Debug.WriteLine($"{t.CanExecute} | {c.CanExecute(null)}");
        t.ID = "514";
        Debug.WriteLine($"{t.CanExecute} | {c.CanExecute(null)}");
        return;
    }

    private static void TestModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TestModel model)
            return;
        if (e.PropertyName == nameof(model.CanExecute))
            Debug.WriteLine(model.CanExecute);
    }
}

partial class TestModel : ReactiveObjectX
{
    public TestModel() { }

    [ReactiveProperty]
    public string ID { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    [NotifyPropertyChangedFrom(nameof(ID), nameof(Name))]
    public bool CanExecute => Name == ID;

    [ReactiveCommand(nameof(CanExecute))]
    public void Test()
    {
        Console.WriteLine(nameof(Test));
    }

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
}
