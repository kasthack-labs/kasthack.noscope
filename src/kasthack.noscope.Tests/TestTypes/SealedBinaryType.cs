namespace kasthack.noscope.Tests.TestTypes;

/// <summary>
/// A sealed class from a "binary dependency" that cannot be made partial.
/// Used for testing ReflectionAccessor.
/// </summary>
public sealed class SealedBinaryType
{
    private int _privateValue = 100;
    private string _privateName = "sealed";

    public SealedBinaryType()
    {
    }

    public SealedBinaryType(int value, string name)
    {
        _privateValue = value;
        _privateName = name;
    }

    public int PublicValue { get; set; } = 200;

    public string GetPrivateName() => _privateName;

    public int GetPrivateValue() => _privateValue;
}
