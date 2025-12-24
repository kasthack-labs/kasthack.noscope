namespace kasthack.noscope;

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
        _getter = getter;
        _setter = setter;
    }

    /// <summary>
    /// Gets the value from the target.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <returns>The member value.</returns>
    public TValue Get(TTarget target)
    {
        if (_getter == null)
        {
            throw new InvalidOperationException("No getter available for this accessor.");
        }

        return _getter(target);
    }

    /// <summary>
    /// Sets the value on the target.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="value">The value to set.</param>
    public void Set(TTarget target, TValue value)
    {
        if (_setter == null)
        {
            throw new InvalidOperationException("No setter available for this accessor.");
        }

        _setter(target, value);
    }
}

/// <summary>
/// Factory methods for creating accessors.
/// </summary>
public static class Accessor
{
    /// <summary>
    /// Creates an accessor from getter and setter functions.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="get">The getter function.</param>
    /// <param name="set">The setter action.</param>
    /// <returns>A new accessor instance.</returns>
    public static Accessor<TTarget, TValue> FromFunc<TTarget, TValue>(
        Func<TTarget, TValue>? get = null,
        Action<TTarget, TValue>? set = null)
    {
        return new Accessor<TTarget, TValue>(get, set);
    }

    /// <summary>
    /// Creates a reflection-based accessor for a field.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="fieldName">The field name.</param>
    /// <returns>A new accessor instance.</returns>
    public static Accessor<TTarget, TValue> ForField<TTarget, TValue>(string fieldName)
    {
        var field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Field '{fieldName}' not found on type '{typeof(TTarget).FullName}'.");

        var targetParam = Expression.Parameter(typeof(TTarget), "target");
        var fieldAccess = Expression.Field(targetParam, field);
        var getter = Expression.Lambda<Func<TTarget, TValue>>(fieldAccess, targetParam).Compile();

        var valueParam = Expression.Parameter(typeof(TValue), "value");
        var assign = Expression.Assign(fieldAccess, valueParam);
        var setter = Expression.Lambda<Action<TTarget, TValue>>(assign, targetParam, valueParam).Compile();

        return new Accessor<TTarget, TValue>(getter, setter);
    }

    /// <summary>
    /// Creates a reflection-based accessor for a property.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <returns>A new accessor instance.</returns>
    public static Accessor<TTarget, TValue> ForProperty<TTarget, TValue>(string propertyName)
    {
        var property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(TTarget).FullName}'.");

        var targetParam = Expression.Parameter(typeof(TTarget), "target");
        var propertyAccess = Expression.Property(targetParam, property);

        Func<TTarget, TValue>? getter = null;
        if (property.CanRead)
        {
            getter = Expression.Lambda<Func<TTarget, TValue>>(propertyAccess, targetParam).Compile();
        }

        Action<TTarget, TValue>? setter = null;
        if (property.CanWrite)
        {
            var valueParam = Expression.Parameter(typeof(TValue), "value");
            var assign = Expression.Assign(propertyAccess, valueParam);
            setter = Expression.Lambda<Action<TTarget, TValue>>(assign, targetParam, valueParam).Compile();
        }

        return new Accessor<TTarget, TValue>(getter, setter);
    }
}
