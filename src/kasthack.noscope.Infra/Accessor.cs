namespace kasthack.noscope.Infra;

using System;
using System.Linq.Expressions;
using System.Reflection;

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

        Action<TTarget, TValue>? setter = null;
        if (!(field.IsInitOnly || field.IsLiteral))
        {
            var valueParam = Expression.Parameter(typeof(TValue), "value");
            var assign = Expression.Assign(fieldAccess, valueParam);
            setter = Expression.Lambda<Action<TTarget, TValue>>(assign, targetParam, valueParam).Compile();
        }

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
