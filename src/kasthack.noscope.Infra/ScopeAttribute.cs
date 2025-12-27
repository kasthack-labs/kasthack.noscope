namespace kasthack.noscope.Infra;

using System;

/// <summary>
/// Marks an interface as a scope facade for the specified target type.
/// The source generator will create a corresponding scope class that implements this interface.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class ScopeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeAttribute"/> class.
    /// </summary>
    /// <param name="targetType">The type to create a scope facade for.</param>
    public ScopeAttribute(Type targetType)
    {
        this.TargetType = targetType;
    }

    /// <summary>
    /// Gets the target type that this scope provides access to.
    /// </summary>
    public Type TargetType { get; }
}
