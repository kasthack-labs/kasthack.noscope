namespace kasthack.noscope.Analyzers;

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzes scope member attributes for correctness.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ScopeMemberAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for missing member.
    /// </summary>
    public const string MissingMemberDiagnosticId = "NS0001";

    /// <summary>
    /// Diagnostic ID for type mismatch.
    /// </summary>
    public const string TypeMismatchDiagnosticId = "NS0002";

    /// <summary>
    /// Diagnostic ID for missing accessor.
    /// </summary>
    public const string MissingAccessorDiagnosticId = "NS0003";

    /// <summary>
    /// Diagnostic ID for using string literal instead of nameof.
    /// </summary>
    public const string UseNameofDiagnosticId = "NS0004";

    private static readonly DiagnosticDescriptor MissingMemberRule = new(
        MissingMemberDiagnosticId,
        "Missing target member",
        "Member '{0}' not found on target type '{1}'",
        "NoScope",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified member does not exist on the target type.");

    private static readonly DiagnosticDescriptor TypeMismatchRule = new(
        TypeMismatchDiagnosticId,
        "Type mismatch",
        "Type mismatch: scope member '{0}' has type '{1}', but target member has type '{2}'",
        "NoScope",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The scope member type does not match the target member type.");

    private static readonly DiagnosticDescriptor MissingAccessorRule = new(
        MissingAccessorDiagnosticId,
        "Missing accessor",
        "Target member '{0}' does not have a {1} accessor",
        "NoScope",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The target member does not have the required accessor.");

    private static readonly DiagnosticDescriptor UseNameofRule = new(
        UseNameofDiagnosticId,
        "Use nameof instead of string literal",
        "Consider using nameof({0}) instead of string literal for refactoring safety",
        "NoScope",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Using nameof() instead of string literals provides refactoring safety.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingMemberRule, TypeMismatchRule, MissingAccessorRule, UseNameofRule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
    }

    private static void AnalyzeInterface(SyntaxNodeAnalysisContext context)
    {
        var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;
        var interfaceSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDecl);
        if (interfaceSymbol is null)
        {
            return;
        }

        var scopeAttribute = GetScopeAttribute(interfaceSymbol);
        if (scopeAttribute is null)
        {
            return;
        }

        var targetType = GetTargetType(scopeAttribute);
        if (targetType is null)
        {
            return;
        }

        foreach (var member in interfaceSymbol.GetMembers())
        {
            AnalyzeMember(context, member, targetType);
        }
    }

    private static AttributeData? GetScopeAttribute(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass?.Name == "ScopeAttribute" ||
                (a.AttributeClass?.IsGenericType == true && a.AttributeClass.OriginalDefinition?.Name == "ScopeAttribute"));
    }

    private static INamedTypeSymbol? GetTargetType(AttributeData scopeAttribute)
    {
        if (scopeAttribute.AttributeClass?.IsGenericType == true)
        {
            return scopeAttribute.AttributeClass.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        }

        if (scopeAttribute.ConstructorArguments.Length > 0)
        {
            return scopeAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
        }

        return null;
    }

    private static void AnalyzeMember(SyntaxNodeAnalysisContext context, ISymbol member, INamedTypeSymbol targetType)
    {
        var scopeMemberAttr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ScopeMemberAttribute");

        string targetMemberName = member.Name;

        if (scopeMemberAttr is not null)
        {
            var nameArg = scopeMemberAttr.NamedArguments.FirstOrDefault(a => a.Key == "Name");
            if (nameArg.Key is not null && nameArg.Value.Value is string explicitName)
            {
                targetMemberName = explicitName;

                var attrSyntax = scopeMemberAttr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
                if (attrSyntax is not null)
                {
                    var nameArgSyntax = attrSyntax.ArgumentList?.Arguments
                        .FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == "Name");

                    if (nameArgSyntax?.Expression is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        var diagnostic = Diagnostic.Create(UseNameofRule, literal.GetLocation(), targetMemberName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        switch (member)
        {
            case IPropertySymbol prop:
                AnalyzePropertyMember(context, prop, targetType, targetMemberName);
                break;
            case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                AnalyzeMethodMember(context, method, targetType, targetMemberName);
                break;
            case IEventSymbol evt:
                AnalyzeEventMember(context, evt, targetType, targetMemberName);
                break;
        }
    }

    private static void AnalyzePropertyMember(SyntaxNodeAnalysisContext context, IPropertySymbol prop, INamedTypeSymbol targetType, string targetMemberName)
    {
        var targetMember = FindMember(targetType, targetMemberName);
        if (targetMember is null)
        {
            var location = prop.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(MissingMemberRule, location, targetMemberName, targetType.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        ITypeSymbol? targetMemberType = null;
        bool hasGetter = false;
        bool hasSetter = false;

        switch (targetMember)
        {
            case IPropertySymbol targetProp:
                targetMemberType = targetProp.Type;
                hasGetter = targetProp.GetMethod is not null;
                hasSetter = targetProp.SetMethod is not null;
                break;
            case IFieldSymbol targetField:
                targetMemberType = targetField.Type;
                hasGetter = true;
                hasSetter = !targetField.IsReadOnly;
                break;
        }

        if (targetMemberType is not null && !SymbolEqualityComparer.Default.Equals(prop.Type, targetMemberType))
        {
            var location = prop.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(TypeMismatchRule, location, prop.Name, prop.Type.ToDisplayString(), targetMemberType.ToDisplayString());
            context.ReportDiagnostic(diagnostic);
        }

        if (prop.GetMethod is not null && !hasGetter)
        {
            var location = prop.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(MissingAccessorRule, location, targetMemberName, "getter");
            context.ReportDiagnostic(diagnostic);
        }

        if (prop.SetMethod is not null && !hasSetter)
        {
            var location = prop.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(MissingAccessorRule, location, targetMemberName, "setter");
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeMethodMember(SyntaxNodeAnalysisContext context, IMethodSymbol method, INamedTypeSymbol targetType, string targetMemberName)
    {
        var targetMethod = targetType.GetMembers(targetMemberName).OfType<IMethodSymbol>().FirstOrDefault();
        if (targetMethod is null)
        {
            var location = method.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(MissingMemberRule, location, targetMemberName, targetType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeEventMember(SyntaxNodeAnalysisContext context, IEventSymbol evt, INamedTypeSymbol targetType, string targetMemberName)
    {
        var targetEvent = targetType.GetMembers(targetMemberName).OfType<IEventSymbol>().FirstOrDefault();
        if (targetEvent is null)
        {
            var location = evt.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(MissingMemberRule, location, targetMemberName, targetType.Name);
            context.ReportDiagnostic(diagnostic);
        }
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
}
