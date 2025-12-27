namespace kasthack.noscope.Tests.TestTypes;

using System;

/// <summary>
/// A test "god object" with various member types for testing scope generation.
/// This type is partial to allow GeneratedAccessor tests.
/// </summary>
public partial class GodObject
{
    private int _privateField = 42;
    private string _privateString = "private";
    private readonly int _readonlyPrivateField = 100;

    public GodObject()
    {
    }

    public GodObject(int initialValue)
    {
        _privateField = initialValue;
        PublicProperty = initialValue;
    }

    /// <summary>
    /// Public property for Direct accessor tests.
    /// </summary>
    public int PublicProperty { get; set; } = 10;

    /// <summary>
    /// Public read-only property.
    /// </summary>
    public int PublicReadOnlyProperty { get; } = 20;

    /// <summary>
    /// Internal property for non-public accessor tests.
    /// </summary>
    internal int InternalProperty { get; set; } = 30;

    /// <summary>
    /// Protected property.
    /// </summary>
    protected int ProtectedProperty { get; set; } = 40;

    /// <summary>
    /// Private property.
    /// </summary>
    private int PrivateProperty { get; set; } = 50;

    /// <summary>
    /// Public method for Direct accessor tests.
    /// </summary>
    public void PublicMethod()
    {
        _privateField++;
    }

    /// <summary>
    /// Public method with return value.
    /// </summary>
    public int PublicMethodWithReturn()
    {
        return _privateField;
    }

    /// <summary>
    /// Public method with parameters.
    /// </summary>
    public int PublicMethodWithParameters(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Public event for Direct accessor tests.
    /// </summary>
    public event EventHandler<int>? ValueChanged;

    /// <summary>
    /// Raises the ValueChanged event.
    /// </summary>
    public void RaiseValueChanged(int value)
    {
        ValueChanged?.Invoke(this, value);
    }

    /// <summary>
    /// Property with ugly name for ScopeMember.Name testing.
    /// </summary>
    public int WhoCoMesUpWiThThEsEnAmEs { get; set; } = 60;
}
