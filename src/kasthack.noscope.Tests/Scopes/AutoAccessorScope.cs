namespace kasthack.noscope.Tests.Scopes;

using kasthack.noscope.Infra;
using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Manual implementation of AutoAccessorScope for testing.
/// Demonstrates Auto accessor selection: public uses Direct, private uses reflection.
/// </summary>
public partial class AutoAccessorScope : IAutoAccessorScope
{
    private readonly GodObject _target;

    private static readonly Accessor<GodObject, int> _accessorPrivateField = Accessor.ForField<GodObject, int>("_privateField");

    public AutoAccessorScope(GodObject target)
    {
        _target = target;
    }

    public GodObject Target => _target;

    // Auto selected Direct accessor for public property
    public int PublicProperty
    {
        get => Target.PublicProperty;
        set => Target.PublicProperty = value;
    }

    // Auto selected GeneratedAccessor/Reflection for private field
    public int _privateField
    {
        get => _accessorPrivateField.Get(Target);
        set => _accessorPrivateField.Set(Target, value);
    }
}
