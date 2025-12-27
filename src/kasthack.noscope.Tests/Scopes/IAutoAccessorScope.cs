namespace kasthack.noscope.Tests.Scopes;

using kasthack.noscope.Infra;
using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Scope interface for testing Auto accessor selection.
/// The generator should automatically pick the best accessor kind.
/// </summary>
[Scope<GodObject>]
public partial interface IAutoAccessorScope
{
    /// <summary>
    /// Public property - Auto should select Direct.
    /// </summary>
    int PublicProperty { get; set; }

    /// <summary>
    /// Private field - Auto should select GeneratedAccessor (GodObject is partial).
    /// </summary>
    int _privateField { get; set; }
}
