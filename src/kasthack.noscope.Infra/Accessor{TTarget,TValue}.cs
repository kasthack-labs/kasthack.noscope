namespace kasthack.noscope.Infra;

using System;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Provides accessor functionality for accessing target members.
/// </summary>
/// <typeparam name="TTarget">The target type.</typeparam>
/// <typeparam name="TValue">The member value type.</typeparam>
public class Accessor<TTarget, TValue>
{
    private readonly Func<TTarget, TValue>? _getter;
    private readonly Action<TTarget, TValue>? _setter;

    /// <summary>
    /// Initializes a new instance of the <see cref="Accessor{TTarget, TValue}"/> class.
    /// </summary>
    /// <param name="getter">The getter function.</param>
    /// <param name="setter">The setter action.</param>
    public Accessor(Func<TTarget, TValue>? getter, Action<TTarget, TValue>? setter)
    {
        this._getter = getter;
        this._setter = setter;
    }

    public bool HasGet => this._getter != null;

    public bool HasSet => this._setter != null;

    /// <summary>
    /// Gets the value from the target.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <returns>The member value.</returns>
    public TValue Get(TTarget target)
    {
        if (this._getter == null)
        {
            throw new InvalidOperationException("No getter available for this accessor.");
        }

        return this._getter(target);
    }

    /// <summary>
    /// Sets the value on the target.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="value">The value to set.</param>
    public void Set(TTarget target, TValue value)
    {
        if (this._setter == null)
        {
            throw new InvalidOperationException("No setter available for this accessor.");
        }

        this._setter(target, value);
    }
}
