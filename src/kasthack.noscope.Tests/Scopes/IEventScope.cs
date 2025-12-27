namespace kasthack.noscope.Tests.Scopes;

using System;

using kasthack.noscope.Infra;
using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Scope interface for testing event support.
/// </summary>
[Scope<GodObject>]
public partial interface IEventScope
{
    /// <summary>
    /// Event from the target.
    /// </summary>
    event EventHandler<int> ValueChanged;
}
