namespace kasthack.noscope.Tests.Scopes;

using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Manual implementation of DirectAccessorScope for testing until source generator is fully integrated.
/// </summary>
public partial class DirectAccessorScope : IDirectAccessorScope
{
    private readonly GodObject _target;

    public DirectAccessorScope(GodObject target)
    {
        _target = target;
    }

    public GodObject Target => _target;

    public int PublicProperty
    {
        get => Target.PublicProperty;
        set => Target.PublicProperty = value;
    }

    public int PublicReadOnlyProperty => Target.PublicReadOnlyProperty;

    public void PublicMethod() => Target.PublicMethod();

    public int PublicMethodWithReturn() => Target.PublicMethodWithReturn();

    public int PublicMethodWithParameters(int a, int b) => Target.PublicMethodWithParameters(a, b);

    public int NiceName
    {
        get => Target.WhoCoMesUpWiThThEsEnAmEs;
        set => Target.WhoCoMesUpWiThThEsEnAmEs = value;
    }
}
