namespace kasthack.noscope.Tests.Scopes;

using System;

using kasthack.noscope.Tests.TestTypes;

/// <summary>
/// Manual implementation of EventScope for testing.
/// </summary>
public partial class EventScope : IEventScope
{
    private readonly GodObject _target;

    public EventScope(GodObject target)
    {
        _target = target;
    }

    public GodObject Target => _target;

    public event EventHandler<int> ValueChanged
    {
        add => Target.ValueChanged += value;
        remove => Target.ValueChanged -= value;
    }
}
