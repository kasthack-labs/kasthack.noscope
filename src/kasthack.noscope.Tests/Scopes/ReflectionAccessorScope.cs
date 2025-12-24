namespace kasthack.noscope.Tests.Scopes;

using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Manual implementation of ReflectionAccessorScope for testing.
/// Uses reflection-based accessors for private members of sealed types.
/// </summary>
public partial class ReflectionAccessorScope : IReflectionAccessorScope
{
    private readonly SealedBinaryType _target;

    private static readonly Accessor<SealedBinaryType, int> _accessorPrivateValue = Accessor.ForField<SealedBinaryType, int>("_privateValue");
    private static readonly Accessor<SealedBinaryType, string> _accessorPrivateName = Accessor.ForField<SealedBinaryType, string>("_privateName");

    public ReflectionAccessorScope(SealedBinaryType target)
    {
        _target = target;
    }

    public SealedBinaryType Target => _target;

    public int PublicValue
    {
        get => Target.PublicValue;
        set => Target.PublicValue = value;
    }

    public int _privateValue
    {
        get => _accessorPrivateValue.Get(Target);
        set => _accessorPrivateValue.Set(Target, value);
    }

    public string _privateName
    {
        get => _accessorPrivateName.Get(Target);
        set => _accessorPrivateName.Set(Target, value);
    }
}
