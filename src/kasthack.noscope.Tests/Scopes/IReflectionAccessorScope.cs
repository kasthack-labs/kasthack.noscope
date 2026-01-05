namespace kasthack.noscope.Tests.Scopes;

using kasthack.noscope.Infra;
using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Scope interface for testing ReflectionAccessor (non-public members on non-partial types).
/// SealedBinaryType cannot be made partial, so reflection must be used.
/// </summary>
[Scope(typeof(SealedBinaryType))]
public partial interface IReflectionAccessorScope
{
    /// <summary>
    /// Public property - should still use Direct accessor.
    /// </summary>
    int PublicValue { get; set; }

    /// <summary>
    /// Private field - forced to use ReflectionAccessor.
    /// </summary>
    [ScopeMember(AccessKind = AccessKind.ReflectionAccessor)]
    int _privateValue { get; set; }

    /// <summary>
    /// Private string field - forced to use ReflectionAccessor.
    /// </summary>
    [ScopeMember(AccessKind = AccessKind.ReflectionAccessor)]
    string _privateName { get; set; }
}
