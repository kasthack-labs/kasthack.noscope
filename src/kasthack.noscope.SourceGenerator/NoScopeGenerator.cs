namespace kasthack.noscope.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using kasthack.noscope.Infra;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Incremental source generator that creates scope facade classes from interfaces marked with [Scope].
/// </summary>
[Generator]
public class NoScopeGenerator : IIncrementalGenerator
{
    /*
     * Flow:
     * - Generator registers a source output that
     *  - monitors [Scope] attributes on interface declarations
     *  - emits ScopeInfo collections
     *  - generates sources from the emitted ScopeInfos
     */
    private static readonly string ScopeMemberAttributeName = nameof(ScopeMemberAttribute);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var scopes = new[] { typeof(ScopeAttribute), typeof(ScopeAttribute<>) }
            .Select(type => type.FullName)
            .Select(typeName => context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    typeName,
                    predicate: static (node, _) => node is InterfaceDeclarationSyntax,
                    transform: static (ctx, _) => GetScopeInfo(ctx))
                .Where(static info => info is not null)
                .Select(static (info, _) => info!)
                .Collect())
            .ToArray();

        var allScopes = scopes[0].Combine(scopes[1]);
        context.RegisterSourceOutput(allScopes, static (spc, scopes) =>
        {
            foreach (var scopeInfo in scopes.Left.Concat(scopes.Right))
            {
                GenerateScopeClass(spc, scopeInfo);
            }
        });
    }

    private static ScopeInfo? GetScopeInfo(GeneratorAttributeSyntaxContext context)
    {
        var interfaceSymbol = context.TargetSymbol as INamedTypeSymbol;
        if (interfaceSymbol is null)
        {
            return null;
        }

        var targetType = GetTargetType(context);
        if (targetType is null)
        {
            return null;
        }

        var members = interfaceSymbol.GetMembers()
            .Select(member => AnalyzeMember(member, targetType))
            .Where(a => a != null)
            .Select(a => a!)
            .ToList();

        return new ScopeInfo(
            Namespace: interfaceSymbol.ContainingNamespace.ToDisplayString(),
            InterfaceName: interfaceSymbol.Name,
            TargetTypeName: targetType.ToDisplayString(),
            TargetType: targetType,
            IsTargetPartial: IsTargetPartial(targetType),
            Members: members);
    }

    private static INamedTypeSymbol? GetTargetType(GeneratorAttributeSyntaxContext context)
    {
        INamedTypeSymbol? targetType = null;

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null)
        {
            return null;
        }

        // we can't extract attribute data from source in a reflection-like way, so we rely on type-parameters or constructor arguments
        // typeof(...) constructor arguments are there for back-compatibility with older C# versions
        if (attribute.AttributeClass?.IsGenericType == true)
        {
            targetType = attribute.AttributeClass.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        }
        else if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is INamedTypeSymbol type)
        {
            targetType = type;
        }

        return targetType;
    }

    private static bool IsTargetPartial(INamedTypeSymbol targetType) =>
        targetType.DeclaringSyntaxReferences
        .Any(syntaxRef => syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl
            && typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static ScopeMemberInfo? AnalyzeMember(ISymbol member, INamedTypeSymbol targetType)
    {
        var (accessKind, targetMemberName) = GetScopeMemberOptions(member);

        return member switch
        {
            IPropertySymbol prop => AnalyzeProperty(prop, targetType, targetMemberName, accessKind),
            IMethodSymbol method when !(method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet) => AnalyzeMethod(method, targetType, targetMemberName, accessKind),
            IEventSymbol evt => AnalyzeEvent(evt, targetType, targetMemberName, accessKind),
            _ => null,
        };
    }

    private static (AccessKind AccessKind, string TargetMemberName) GetScopeMemberOptions(ISymbol member)
    {
        string? explicitName = null;
        var accessKind = AccessKind.Auto;
        var scopeMemberAttr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == ScopeMemberAttributeName);
        if (scopeMemberAttr is not null)
        {
            foreach (var namedArg in scopeMemberAttr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Name":
                        explicitName = namedArg.Value.Value as string;
                        break;
                    case "AccessKind":
                        accessKind = (AccessKind)(int)namedArg.Value.Value!;
                        break;
                }
            }
        }
        return (accessKind, explicitName ?? member.Name);
    }

    // TODO: bug? doesn't account for default method implementations
    // TODO: poor design. resolves targets too early which can be an performance issue
    // TODO: check how it interacts with binary dependencies
    private static ScopeMemberInfo? AnalyzeProperty(IPropertySymbol prop, INamedTypeSymbol targetType, string targetMemberName, AccessKind accessKind)
    {
        var targetMember = FindMember(targetType, targetMemberName);
        if (targetMemberName is null)
        {
            return new ScopeMemberInfo(
                MemberKind.Property,
                prop.Name,
                prop.Type.ToDisplayString(),
                targetMemberName,
                accessKind,
                prop.GetMethod is not null,
                prop.SetMethod is not null,
                ImmutableArray<ParameterInfo>.Empty,
                false,
                $"Member '{targetMemberName}' not found on target type");
        }

        var (memberType, canRead, canWrite) = targetMember switch
        {
            IPropertySymbol targetProp => (targetProp.Type.ToDisplayString(), targetProp.GetMethod is not null, targetProp.SetMethod is not null),
            IFieldSymbol targetField => (targetField.Type.ToDisplayString(), true, !targetField.IsReadOnly),
            _ => (null, false, false),
        };

        if (memberType is null)
        {
            return new ScopeMemberInfo(MemberKind.Property, prop.Name, prop.Type.ToDisplayString(), targetMemberName, accessKind, prop.GetMethod is not null, prop.SetMethod is not null, ImmutableArray<ParameterInfo>.Empty, false, $"Member '{targetMemberName}' is not a property or field");
        }

        var isPublic = targetMember.DeclaredAccessibility == Accessibility.Public;

        return new ScopeMemberInfo(
            MemberKind.Property,
            prop.Name,
            prop.Type.ToDisplayString(),
            targetMemberName,
            accessKind,
            prop.GetMethod is not null && canRead,
            prop.SetMethod is not null && canWrite,
            ImmutableArray<ParameterInfo>.Empty,
            isPublic,
            null);
    }

    // same issue as with properties
    private static ScopeMemberInfo? AnalyzeMethod(IMethodSymbol method, INamedTypeSymbol targetType, string targetMemberName, AccessKind accessKind)
    {
        var targetMethod = targetType.GetMembers(targetMemberName).OfType<IMethodSymbol>().FirstOrDefault();
        if (targetMethod is null)
        {
            return new ScopeMemberInfo(
                Kind: MemberKind.Method,
                Name: method.Name,
                TypeName: method.ReturnType.ToDisplayString(),
                TargetMemberName: targetMemberName,
                RequestedAccessKind: accessKind,
                HasGetter: false,
                HasSetter: false,
                Parameters: GetParameters(method),
                IsPublic: false,
                Error: $"Method '{targetMemberName}' not found on target type");
        }

        var isPublic = targetMethod.DeclaredAccessibility == Accessibility.Public;

        return new ScopeMemberInfo(
            MemberKind.Method,
            method.Name,
            method.ReturnType.ToDisplayString(),
            targetMemberName,
            accessKind,
            false,
            false,
            GetParameters(method),
            isPublic,
            null);
    }

    // TODO: same as above
    private static ScopeMemberInfo? AnalyzeEvent(IEventSymbol evt, INamedTypeSymbol targetType, string targetMemberName, AccessKind accessKind)
    {
        var targetEvent = targetType.GetMembers(targetMemberName).OfType<IEventSymbol>().FirstOrDefault();
        if (targetEvent is null)
        {
            return new ScopeMemberInfo(MemberKind.Event, evt.Name, evt.Type.ToDisplayString(), targetMemberName, accessKind, false, false, ImmutableArray<ParameterInfo>.Empty, false, $"Event '{targetMemberName}' not found on target type");
        }

        var isPublic = targetEvent.DeclaredAccessibility == Accessibility.Public;

        return new ScopeMemberInfo(
            MemberKind.Event,
            evt.Name,
            evt.Type.ToDisplayString(),
            targetMemberName,
            accessKind,
            false,
            false,
            ImmutableArray<ParameterInfo>.Empty,
            isPublic,
            null);
    }

    private static ImmutableArray<ParameterInfo> GetParameters(IMethodSymbol method)
    {
        var builder = ImmutableArray.CreateBuilder<ParameterInfo>(method.Parameters.Length);
        foreach (var param in method.Parameters)
        {
            builder.Add(new ParameterInfo(param.Name, param.Type.ToDisplayString(), param.RefKind));
        }

        return builder.ToImmutable();
    }

    private static ISymbol? FindMember(INamedTypeSymbol type, string name)
    {
        var current = type;
        while (current is not null)
        {
            foreach (var member in current.GetMembers(name))
            {
                if (member is IPropertySymbol or IFieldSymbol)
                {
                    return member;
                }
            }

            current = current.BaseType;
        }

        return null;
    }

    // TODO: likely broken. Multiple scopes will generate duplicate accessors
    private static void GenerateScopeClass(SourceProductionContext context, ScopeInfo scopeInfo)
    {
        var sb = new StringBuilder();
        var className = scopeInfo.InterfaceName.StartsWith("I", StringComparison.Ordinal)
            ? scopeInfo.InterfaceName.Substring(1)
            : scopeInfo.InterfaceName + "Impl";

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(scopeInfo.Namespace) && scopeInfo.Namespace != "<global namespace>")
        {
            sb.AppendLine($"namespace {scopeInfo.Namespace};");
            sb.AppendLine();
        }

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Generated scope class for <see cref=\"{scopeInfo.InterfaceName}\"/>.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public partial class {className} : {scopeInfo.InterfaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {scopeInfo.TargetTypeName} _target;");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Initializes a new instance of the <see cref=\"{className}\"/> class.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    /// <param name=\"target\">The target object to wrap.</param>");
        sb.AppendLine($"    public {className}({scopeInfo.TargetTypeName} target)");
        sb.AppendLine("    {");
        sb.AppendLine("        _target = target;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Gets the underlying target object.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public virtual {scopeInfo.TargetTypeName} Target => _target;");

        var needsAccessors = new List<ScopeMemberInfo>();

        foreach (var member in scopeInfo.Members)
        {
            if (member.Error is not null)
            {
                continue;
            }

            var effectiveAccessKind = DetermineAccessKind(member, scopeInfo.IsTargetPartial);

            switch (member.Kind)
            {
                case MemberKind.Property:
                    GenerateProperty(sb, member, effectiveAccessKind, scopeInfo);
                    if (effectiveAccessKind == AccessKind.GeneratedAccessor || effectiveAccessKind == AccessKind.ReflectionAccessor)
                    {
                        needsAccessors.Add(member);
                    }

                    break;
                case MemberKind.Method:
                    GenerateMethod(sb, member, effectiveAccessKind);
                    break;
                case MemberKind.Event:
                    GenerateEvent(sb, member, effectiveAccessKind);
                    break;
            }
        }

        if (needsAccessors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("    #region Accessors");
            foreach (var member in needsAccessors)
            {
                var effectiveAccessKind = DetermineAccessKind(member, scopeInfo.IsTargetPartial);
                GenerateAccessorField(sb, member, effectiveAccessKind, scopeInfo);
            }

            sb.AppendLine("    #endregion");
        }

        sb.AppendLine("}");

        context.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

        if (scopeInfo.IsTargetPartial && needsAccessors.Any(m => DetermineAccessKind(m, true) == AccessKind.GeneratedAccessor))
        {
            GenerateTargetAccessors(context, scopeInfo, needsAccessors);
        }
    }

    private static AccessKind DetermineAccessKind(ScopeMemberInfo member, bool isTargetPartial)
    {
        if (member.RequestedAccessKind != AccessKind.Auto)
        {
            return member.RequestedAccessKind;
        }

        if (member.IsPublic)
        {
            return AccessKind.Direct;
        }

        if (isTargetPartial)
        {
            return AccessKind.GeneratedAccessor;
        }

        return AccessKind.ReflectionAccessor;
    }

    private static void GenerateProperty(StringBuilder sb, ScopeMemberInfo member, AccessKind accessKind, ScopeInfo scopeInfo)
    {
        sb.AppendLine();
        sb.AppendLine($"    /// <inheritdoc/>");
        sb.Append($"    public {member.TypeName} {member.Name}");
        sb.AppendLine();
        sb.AppendLine("    {");

        if (member.HasGetter)
        {
            switch (accessKind)
            {
                case AccessKind.Direct:
                    sb.AppendLine($"        get => Target.{member.TargetMemberName};");
                    break;
                case AccessKind.GeneratedAccessor:
                    sb.AppendLine($"        get => {scopeInfo.TargetTypeName}.NoScopeAccessors.{GetAccessorName(member)}.Get(Target);");
                    break;
                case AccessKind.ReflectionAccessor:
                    sb.AppendLine($"        get => {GetAccessorFieldName(member)}.Get(Target);");
                    break;
            }
        }

        if (member.HasSetter)
        {
            switch (accessKind)
            {
                case AccessKind.Direct:
                    sb.AppendLine($"        set => Target.{member.TargetMemberName} = value;");
                    break;
                case AccessKind.GeneratedAccessor:
                    sb.AppendLine($"        set => {scopeInfo.TargetTypeName}.NoScopeAccessors.{GetAccessorName(member)}.Set(Target, value);");
                    break;
                case AccessKind.ReflectionAccessor:
                    sb.AppendLine($"        set => {GetAccessorFieldName(member)}.Set(Target, value);");
                    break;
            }
        }

        sb.AppendLine("    }");
    }

    private static void GenerateMethod(StringBuilder sb, ScopeMemberInfo member, AccessKind accessKind)
    {
        sb.AppendLine();
        sb.AppendLine($"    /// <inheritdoc/>");

        var parameters = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
        var arguments = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));

        var returnType = member.TypeName;
        var isVoid = returnType == "void";

        sb.Append($"    public {returnType} {member.Name}({parameters})");

        if (accessKind == AccessKind.Direct)
        {
            if (isVoid)
            {
                sb.AppendLine($" => Target.{member.TargetMemberName}({arguments});");
            }
            else
            {
                sb.AppendLine($" => Target.{member.TargetMemberName}({arguments});");
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("    {");
            sb.AppendLine($"        throw new System.NotSupportedException(\"Non-public methods require reflection-based invocation which is not yet implemented.\");");
            sb.AppendLine("    }");
        }
    }

    private static void GenerateEvent(StringBuilder sb, ScopeMemberInfo member, AccessKind accessKind)
    {
        sb.AppendLine();
        sb.AppendLine($"    /// <inheritdoc/>");

        if (accessKind == AccessKind.Direct)
        {
            sb.AppendLine($"    public event {member.TypeName} {member.Name}");
            sb.AppendLine("    {");
            sb.AppendLine($"        add => Target.{member.TargetMemberName} += value;");
            sb.AppendLine($"        remove => Target.{member.TargetMemberName} -= value;");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine($"    public event {member.TypeName}? {member.Name};");
        }
    }

    private static void GenerateAccessorField(StringBuilder sb, ScopeMemberInfo member, AccessKind accessKind, ScopeInfo scopeInfo)
    {
        if (accessKind == AccessKind.ReflectionAccessor)
        {
            var isField = member.TargetMemberName.StartsWith("_", StringComparison.Ordinal);
            var accessorMethod = isField ? "ForField" : "ForProperty";
            sb.AppendLine($"    private static readonly kasthack.noscope.Accessor<{scopeInfo.TargetTypeName}, {member.TypeName}> {GetAccessorFieldName(member)} = kasthack.noscope.Accessor.{accessorMethod}<{scopeInfo.TargetTypeName}, {member.TypeName}>(\"{member.TargetMemberName}\");");
        }
    }

    private static void GenerateTargetAccessors(SourceProductionContext context, ScopeInfo scopeInfo, List<ScopeMemberInfo> members)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var targetNamespace = scopeInfo.TargetType.ContainingNamespace.ToDisplayString();
        if (!string.IsNullOrEmpty(targetNamespace) && targetNamespace != "<global namespace>")
        {
            sb.AppendLine($"namespace {targetNamespace};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {scopeInfo.TargetType.Name}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Generated accessors for NoScope facade.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static partial class NoScopeAccessors");
        sb.AppendLine("    {");

        foreach (var member in members.Where(m => DetermineAccessKind(m, true) == AccessKind.GeneratedAccessor))
        {
            var accessorName = GetAccessorName(member);
            sb.AppendLine($"        /// <summary>Accessor for {member.TargetMemberName}.</summary>");
            sb.Append($"        public static kasthack.noscope.Accessor<{scopeInfo.TargetTypeName}, {member.TypeName}> {accessorName} {{ get; }} = kasthack.noscope.Accessor.FromFunc<{scopeInfo.TargetTypeName}, {member.TypeName}>(");

            if (member.HasGetter)
            {
                sb.Append($"get: target => target.{member.TargetMemberName}");
            }

            if (member.HasGetter && member.HasSetter)
            {
                sb.Append(", ");
            }

            if (member.HasSetter)
            {
                sb.Append($"set: (target, value) => target.{member.TargetMemberName} = value");
            }

            sb.AppendLine(");");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource($"{scopeInfo.TargetType.Name}.NoScopeAccessors.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static string GetAccessorName(ScopeMemberInfo member)
    {
        var name = member.TargetMemberName;
        if (name.StartsWith("_", StringComparison.Ordinal))
        {
            name = name.Substring(1);
        }

        return $"AccessorFor{char.ToUpperInvariant(name[0])}{name.Substring(1)}";
    }

    private static string GetAccessorFieldName(ScopeMemberInfo member)
    {
        return $"_accessor_{member.Name}";
    }

    private static string FormatParameter(ParameterInfo param)
    {
        var prefix = param.RefKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => string.Empty,
        };
        return $"{prefix}{param.TypeName} {param.Name}";
    }

    private static string FormatArgument(ParameterInfo param)
    {
        var prefix = param.RefKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => string.Empty,
        };
        return $"{prefix}{param.Name}";
    }
}
