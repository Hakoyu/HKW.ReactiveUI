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
        return;
    }
}

partial class TestModel : ReactiveObject
{
    public TestModel() { }

    [ReactiveProperty]
    public string Name { get; set; } = string.Empty;

    public bool CanExecute => string.IsNullOrWhiteSpace(Name);

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
    public async Task Test1Async()
    {
        await Task.Delay(1000);
        Console.WriteLine(nameof(Test1Async));
    }
}
