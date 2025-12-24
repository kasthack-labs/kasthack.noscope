namespace kasthack.noscope.Tests.Scopes;

using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Scope interface for testing Direct accessors (public members only).
/// </summary>
[Scope(typeof(GodObject))]
public partial interface IDirectAccessorScope
{
    /// <summary>
    /// Public property - should use Direct accessor.
    /// </summary>
    int PublicProperty { get; set; }

    /// <summary>
    /// Public read-only property.
    /// </summary>
    int PublicReadOnlyProperty { get; }

    /// <summary>
    /// Public method.
    /// </summary>
    void PublicMethod();

    /// <summary>
    /// Public method with return.
    /// </summary>
    int PublicMethodWithReturn();

    /// <summary>
    /// Public method with parameters.
    /// </summary>
    int PublicMethodWithParameters(int a, int b);

    /// <summary>
    /// Renamed property using ScopeMember.
    /// </summary>
    [ScopeMember(Name = nameof(GodObject.WhoCoMesUpWiThThEsEnAmEs))]
    int NiceName { get; set; }
}
