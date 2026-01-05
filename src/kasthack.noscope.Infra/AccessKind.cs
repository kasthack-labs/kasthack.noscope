namespace kasthack.noscope.Infra;

/// <summary>
/// Specifies the kind of accessor to use for a scope member.
/// </summary>
public enum AccessKind
{
    /// <summary>
    /// Automatically selects the best accessor kind based on member accessibility.
    /// Tries Direct first, then GeneratedAccessor, then ReflectionAccessor.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Uses direct property/method access. Only works for public members.
    /// </summary>
    Direct = 1,

    /// <summary>
    /// Generates compile-time accessors. Requires the target type to be partial.
    /// Works for non-public members in source-available types.
    /// </summary>
    GeneratedAccessor = 2,

    /// <summary>
    /// Uses reflection-based runtime code generation.
    /// Works for private members from binary dependencies.
    /// </summary>
    ReflectionAccessor = 3,
}
