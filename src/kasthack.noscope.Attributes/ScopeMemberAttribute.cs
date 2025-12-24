namespace kasthack.noscope;

using System;

/// <summary>
/// Configures how a scope member maps to the target type's member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = false)]
public class ScopeMemberAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the target member to access.
    /// When not specified, the scope member name is used.
    /// Use nameof() when possible for refactoring safety.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the kind of accessor to use for this member.
    /// Defaults to <see cref="AccessKind.Auto"/>.
    /// </summary>
    public AccessKind AccessKind { get; set; } = AccessKind.Auto;
}
