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

Create an interface marked with `Scope(typeof(T))` attribute that specifies the proxied type and triggers the generation of a facade class in the same namespace:

```csharp

// there can only be one scope target attribute per scope interface
[Scope(typeof(GodObject))]
partial interface ISomeServiceScope
{
    // source generator will generate a protected Target proprety, so you could use it in custom methods
    // generally, you don't need to access it directly and you should rely on implicit property names or scopeMember attributes

    // points to Target.IntegerProperty which can be both a property or a field
    // if there's no matching property, we get the corresponding error
    // if there're no matching accessors, we get an error as well
    // types get checked by the analyzer as well
    int IntegerProperty {get;}

    // facade library can access private fields too.
    // use [ScopeMember(Name = ...)] to expose properties with conventional names instead of relying on private property name matching.
    int _privateIntegerProperty {get;set;}

    // or rename properties
    // 'Name' is special-cased to issue warnings when an accessible member
    // is addressed not with a nameof() statement.

    // an included roslyn analyzer will throw a compilation error the same way as it does for prroperties with implicit targets on missing properties / type mismatch
    // using nameof is preferred. However, you can use string literals if you're creating a scope for private members of binary dependencies
    [ScopeMember(Name = nameof(GodObject.WhoCoMesUpWiThThEsEnAmEs))]
    int NiceName {get;}

    // ScopeMember also exposes a property for fine-tuning accessor generation
    [ScopeMember(AccessKind = AccessKind.(Auto/Direct/[GeneratedAccessor]/ReflectionAccessor)]
    int _anotherPrivateProperty {get;}


    // proxy methods
    void DoSomething();

    // events are supported to BUT they'll expose the original sender by default.
    // this is an intentional design decision to keep event type compatibility
    // create an event with overridden add / remove to change that
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
    }
}
```

This generates the following class:

```csharp

partial class SomeServiceScope(GodObject target) : ISomeServiceScope
{
    // intentionally exposed. Concrete scopes should not be passed to consumers(pass by interface), but this helps with writing tests.
     public virtual TTarget Target => target;
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
someService.DoSomething(new SomeServiceScope(godObject))

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

## Implementation details

### Accessors

NoScope supports multiple accessor kinds, each designed to support different use-cases:

- Direct. Dead-simple accessor for simple cases when your properties and methods are public that just bypasses the calls. Generates the following code:

```csharp
...scope definition
{
    Type PropertyName
    {
        get => Target.PropertyName;
        set => Target.PropertyName = value;
    }
    Type2 MethodName(... parameters) => Target.MethodName(... parameters);
}
```

- Generated accessor. Compile-time accessor for accessing non-public members in source-available types. Requires the target type to be partial due to source generator limitations.


```csharp
//target we're generating a scope for:
class TargetType
{
    private int _value;
}

// scope interface
[Scope<TargetType>]
interface IValueScope
{
    public int _value {get;}
}

// generated code:

partial class TargetType
{

    partial class NoScopeAccessors
    {
        public static Accessor<Target, int> AccessorFor_value {get;} = Accessor.FromFunc(get: target => target._value, set: (target, value) => target._value = value);
    }
}

class ValueScope : IValueScope
{
    ... constructor and props
    public int _value => TargetTypeWithNamespaces.NoScopeAccessors.AccessorFor_value.Get(Target);
}
```

- Reflection Accessor. Handles private properties from binary dependencies. Uses compiled linq expressions.

Source generator normally switches between accessor strategies automatically(first, it tries direct, then accessors, then reflection-based runtime code generation), but you can make it use any option for your edge-case.