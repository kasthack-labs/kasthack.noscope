# kasthack.noscope

## What

NoScope is a source generator library for building scoped facades that assist with extending and/or refactoring tightly coupled types / god objects:

* you don't have to manually write boilerplate like separate interfaces/constructors/getters/setters/proxy methods
* scopes can bypass standard accessibility modifiers, so extracting code that references private/protected/internal members becomes easier

[![Github All Releases](https://img.shields.io/github/downloads/kasthack-labs/kasthack.noscope/total.svg)](https://github.com/kasthack-labs/kasthack.noscope/releases/latest)
[![GitHub release](https://img.shields.io/github/release/kasthack-labs/kasthack.noscope.svg)](https://github.com/kasthack-labs/kasthack.noscope/releases/latest)
[![license](https://img.shields.io/github/license/kasthack-labs/kasthack.noscope.svg)](LICENSE)
[![.NET Status](https://github.com/kasthack-labs/kasthack.noscope/workflows/.NET/badge.svg)](https://github.com/kasthack-labs/kasthack.noscope/actions?query=workflow%3A.NET)
[![Patreon pledges](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.vercel.app%2Fapi%3Fusername%3Dkasthack%26type%3Dpledges&style=flat)](https://patreon.com/kasthack)
[![Patreon patrons](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.vercel.app%2Fapi%3Fusername%3Dkasthack%26type%3Dpatrons&style=flat)](https://patreon.com/kasthack)

## Why does this exist

I had *a lot of fun* with refactoring god objects at work.

## Usage

So, you have a massive god object that you want to use in your code without introducing an unmockable dependency, or you want to break it up.

NoScope comes to the  rescue:

### Step 1: create a scoped facade

Building a scoped facade starts with writing an interface that contains the parts of god object that your code uses.

Create an interface inherited from `IFacade<T>` that specifies the proxied type and triggers the generation of a facade class in the same namespace:

```csharp

/*
interface IFacade<T>
{
	#pragma warning disable
	[Obsolete($"Avoid using {nameof(Target)} directly and pull properties into the facade whenever possible")]
	#pragma warning restore
    protected T Target {get;set;}
}
*/

interface ISomeServiceScope : IFacade<GodObject>
{
    // points to Target.IntegerProperty which can be both a property or a field
    // if there's no matching property, we get the corresponding error
    // if there're no matching accessors, we get an error as well
    int IntegerProperty {get;}

    // facade library can access private fields too
    // it relies on reflection for facade generation by default
    int _privateIntegerProperty {get;set;}

    // but we can also use accessors to be reflection-free at runtime
    // target class must be partial and source-available for the generator to work
    [FacadeMember(AccessKind = AccessKind.(Auto/Direct/[GeneratedAccessor]/ReflectionAccessor)]
    int _anotherPrivateProperty {get;}

    // or rename properties
    // 'Name' is special-cased to issue warnings when an accessible member
    // is addressed not with a nameof() statement
    [FacadeMember(Name = nameof(GodObject.WhoCoMesUpWiThThEsEnAmEs))]
    int NiceName {get;}

    // proxy methods
    void DoSomething();

    // events are supported to BUT they'll expose the original sender by default
    event EventHandler<SomeEvent> Event;

    // provide a view to only expose a derived property using a protected property
    // and a default interface method
    protected int [] Collection {get;}

    // provide a boolean property instead of exposing underlying collection
    bool HasCollectionItems => Collection?.Any() ?? false;

    // or provide a readonly view of the collection
    IReadOnlyList<int> ReadOnlyCollection => new ReadOnlyList<int>(Collection);

    // provide custom methods
    void Dispose()
    {
        Target.Flush();
        Target.Dispose();
        Target = null;
    }
}
```

This generates the following class:

```csharp

/*
public abstract class Facade<TTarget, TFacade>
{
    public virtual TTarget Target {set;}
    protected TFacade Facade => this as TFacade;
    public Facade(TTarget target)
    {
        Target = target;
    }
}
*/

partial class SomeServiceScope(GodObject target) : Facade<GodObject, ISomeServiceScope>(target), ISomeServiceScope
{
    /*
        Your properties and methods go here.
    */
}
```

You can add extra methods to the facade or override its behavior by writing another segment of a partial class or override some behavior:

```csharp

// extend scope with stateful methods
partial class SomeServiceScope : ISomeServiceScope, IExtraInterface
{
    private int _callCount;
    public int ExtraInterfacesProperty => ++_callCount;
}

// override behavior that you couldn't override using default interface methods
partial class ExtendedScope : SomeServiceScope
{
    private bool _disposed;
    public override void Dispose()
    {
        if (_disposed) return;

        Target.Flush();
        Target.Dispose();
        _disposed = true;
    }

    public override GodObject Target => !_disposed ? base.Target : throw new ObjectDisposedException();
}
```

### Step 2: use it in you service

Now, you can use the chiseled type in your code:

```csharp
class SomeService
{
    public void DoSomething(ISomeServiceScope scope)
    {
        var value = scope.ReadOnlyProperty;
        scope.WriteOnlyProperty = value + 1;
        scope.ReadWriteProperty += $"added {value}\n";
        scope.Execute();
    }
}
````

### Step 3: wrap you god objects in a scope

```csharp

// use somewhere in your code
someService.DoSomething(new ISomeServiceScope(godObject))

// or as a sprout class where you've just moved a part of the dreadful god object

class GodObject
{
    public void BackCompatibleMethod()
        => new LightweightSproutClass(new SproutClassScope(this)).BackCompatibleMethod();
}

// or an injected service

class GodObject(NewService scopedService)
{
    public void DoSomething() => scopedService.DoSomething(new NewServiceScope(this));
}

```


### Step 4: test your class with mocks

This example uses NSubstitute, but there's no dependency on it.

```csharp

// arrange
var someService = new SomeService()
var scope = Substitute.For<ISomeServiceScope>();

// act
someService.DoSomething(scope);

// assert
scope.Received().Execute();
```
