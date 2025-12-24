namespace kasthack.noscope.SourceGenerator;

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

/// <summary>
/// Contains information about a scope interface to generate.
/// </summary>
internal sealed record ScopeInfo(
    string Namespace,
    string InterfaceName,
    string TargetTypeName,
    INamedTypeSymbol TargetType,
    bool IsTargetPartial,
    List<ScopeMemberInfo> Members);

/// <summary>
/// Contains information about a member of the scope interface.
/// </summary>
internal sealed record ScopeMemberInfo(
    MemberKind Kind,
    string Name,
    string TypeName,
    string TargetMemberName,
    AccessKind RequestedAccessKind,
    bool HasGetter,
    bool HasSetter,
    ImmutableArray<ParameterInfo> Parameters,
    bool IsPublic,
    string? Error);

/// <summary>
/// Represents a method parameter.
/// </summary>
internal sealed record ParameterInfo(string Name, string TypeName, RefKind RefKind);

/// <summary>
/// Kind of scope member.
/// </summary>
internal enum MemberKind
{
    Property,
    Method,
    Event,
}

/// <summary>
/// Mirror of the AccessKind enum from attributes.
/// </summary>
internal enum AccessKind
{
    Auto = 0,
    Direct = 1,
    GeneratedAccessor = 2,
    ReflectionAccessor = 3,
}
