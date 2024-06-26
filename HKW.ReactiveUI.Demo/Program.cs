using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using ReactiveUI;

namespace HKW.HKWReactiveUI.Demo;

internal class Program
{
    //private string $Name;
    //public string Name { get; set; } = string.Empty;


    static void Main(string[] args)
    {
        var t = new TestModel();
        t.PropertyChanged += TestModel_PropertyChanged;
        t.ID = "114";
        t.Name = "514";
        t.ID = "514";
        return;
    }

    private static void TestModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
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

    [ReactiveProperty]
    public string Name { get; set; } = string.Empty;

    [NotifyPropertyChangedFrom(nameof(ID), nameof(Name))]
    public bool CanExecute => Name == ID;

    public bool CanExecuteM()
    {
        return false;
    }

    [ReactiveCommand(CanExecute = nameof(CanExecute))]
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
