# HKW.ReactiveUI

An MVVM library, based on ReactiveUI, using SourceGenerator and Fody for mixed source code generation.

## ReactivePropertyAttribute

Adding Notifications to Properties Using Fody and SourceGenerator

```csharp
partial class MyViewModel : ReactiveObject
{
    [ReactiveProperty]
    public string Name { get; set; } = string.Empty;
}
```

Generated code

```csharp
partial class MyViewModel : ReactiveObject
{
    private string $Name;

    [ReactiveProperty]
    public string Name
    {
        get => $Name;
        set => RaiseAndSetName(ref $Name, value, true);
    }

    private void RaiseAndSetName(ref string backingField, string newValue, bool check = true)
	{
	    if (!check || !EqualityComparer<string>.Default.Equals(backingField, newValue))
	    {
		    string oldValue = backingField;
            this.RaisePropertyChanging("Name");
            backingField = newValue;
		    this.RaisePropertyChanged("Name");
        }
	}
}
```

## ReactiveCommandAttribute

Adding Command Properties to Methods Using the Source Generator

```csharp
partial class MyViewModel : ReactiveObject
{
    [ReactiveCommand]
    public void Test() { }

    [ReactiveCommand]
    public async Task TestAsync() { }
}
```

Generated code

```csharp
partial class MyViewModel : ReactiveObject
{
    private ReactiveCommand<Unit, Unit> _testCommand;
    public ReactiveCommand<Unit, Unit> TestCommand =>
        _testCommand ?? (_testCommand = ReactiveCommand.Create(Test));
    private ReactiveCommand<Unit, Unit> _testAsyncCommand;
    public ReactiveCommand<Unit, Unit> Test1AsyncCommand =>
        _testAsyncCommand ?? (_testAsyncCommand = ReactiveCommand.CreateFromTask(TestAsync));

    [ReactiveCommand]
    public void Test() { }

    [ReactiveCommand]
    public async Task TestAsync() { }
}
```

## NotifyPropertyChangeFromAttribute

Notify property when target property changed
A field is generated to cache the value when `EnableCache` is true


```csharp
partial class MyViewModel : ReactiveObject
{
    [NotifyPropertyChangeFrom(true, nameof(ID), nameof(Name))]
    public string IsSame => ID == Name;
    protected void InitializeReactiveObject() { }
}
```

Generated code

```csharp
partial class MyViewModel : ReactiveObject
{
    private bool _isSame;
    [NotifyPropertyChangeFrom(nameof(ID), nameof(Name))]
    public string IsSame => Name == ID;

    protected void InitializeReactiveObject()
    {
        // InitializeInInitializeObject = true
       _isSame = Name == ID;
    }
	protected void RaiseIsSameChange()
    {
       this.RaiseAndSetIfChanged(ref _isSame, Name == ID, "IsSame");
    }
    private void RaiseAndSetName(ref string backingField, string newValue, bool check = true)
    {
        ...
        this.RaisePropertyChanged("Name");
        RaiseIsSameChange();
    }
    private void RaiseAndSetID(ref string backingField, string newValue, bool check = true)
    {
        ...
        this.RaisePropertyChanged("ID");
        RaiseIsSameChange();
    }
}
```

---
When `EnableCache` is false

```csharp
partial class MyViewModel : ReactiveObject
{
    [NotifyPropertyChangeFrom(nameof(ID), nameof(Name), EnableCache = false)]
    public string IsSame => ID == Name;
    protected void InitializeReactiveObject() { }
}
```

Generated code

```csharp
partial class MyViewModel : ReactiveObject
{
    private bool _isSame;
    [NotifyPropertyChangeFrom(true ,nameof(ID), nameof(Name), EnableCache = false)]
    public string IsSame => Name == ID;

    protected void InitializeReactiveObject()
    {
        // InitializeInInitializeObject = true
       _isSame = Name == ID;
    }
	protected void RaiseIsSameChange()
    {
       this.RaiseAndSetIfChanged(ref _isSame, Name == ID, "IsSame");
    }
    private void RaiseAndSetName(ref string backingField, string newValue, bool check = true)
    {
        ...
        this.RaisePropertyChanging("Name");
        this.RaisePropertyChanging("IsSame");
        backingField = newValue;
        this.RaisePropertyChanged("Name");
        this.RaisePropertyChanged("IsSame");
        ...
    }
    private void RaiseAndSetID(ref string backingField, string newValue, bool check = true)
    {
        ...
        this.RaisePropertyChanging("ID");
        this.RaisePropertyChanging("IsSame");
        backingField = newValue;
        this.RaisePropertyChanged("ID");
        this.RaisePropertyChanged("IsSame");
        ...
    }
}