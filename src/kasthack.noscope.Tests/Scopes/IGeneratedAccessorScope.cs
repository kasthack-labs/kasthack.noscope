namespace kasthack.noscope.Tests.Scopes;

using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Scope interface for testing GeneratedAccessor (non-public members on partial types).
/// Since GodObject is partial, these should use generated accessors.
/// </summary>
[Scope<GodObject>]
public partial interface IGeneratedAccessorScope
{
    /// <summary>
    /// Private field - should use GeneratedAccessor since GodObject is partial.
    /// </summary>
    [ScopeMember(AccessKind = AccessKind.GeneratedAccessor)]
    int _privateField { get; set; }

    /// <summary>
    /// Private string field.
    /// </summary>
    [ScopeMember(AccessKind = AccessKind.GeneratedAccessor)]
    string _privateString { get; set; }

    /// <summary>
    /// Readonly private field - getter only.
    /// </summary>
    [ScopeMember(AccessKind = AccessKind.GeneratedAccessor)]
    int _readonlyPrivateField { get; }
}
