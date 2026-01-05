namespace kasthack.noscope.Infra;

using System;

/// <summary>
/// Generic version of <see cref="ScopeAttribute"/> for cleaner syntax.
/// </summary>
/// <typeparam name="T">The type to create a scope facade for.</typeparam>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class ScopeAttribute<T> : ScopeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeAttribute{T}"/> class.
    /// </summary>
    public ScopeAttribute()
        : base(typeof(T))
    {
    }
}
