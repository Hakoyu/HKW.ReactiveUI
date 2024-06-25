using ReactiveUI;

namespace HKW.HKWReactiveUI.Demo;

internal class Program
{
    static void Main(string[] args)
    {
        var t = new TestModel();
    }
}

internal partial class TestModel : ReactiveObjectX
{
    public TestModel() { }

    [ReactiveProperty]
    public bool CanExecute { get; set; }

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
