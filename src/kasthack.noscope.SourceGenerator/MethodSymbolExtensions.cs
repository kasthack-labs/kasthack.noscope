namespace kasthack.noscope.SourceGenerator;

using Microsoft.CodeAnalysis;

/// <summary>
/// Extension methods for IMethodSymbol.
/// </summary>
internal static class MethodSymbolExtensions
{
    /// <summary>
    /// Determines if the method is a property accessor (getter or setter).
    /// </summary>
    /// <param name="method">The method symbol.</param>
    /// <returns>True if the method is a property accessor.</returns>
    public static bool IsPropertyAccessor(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.PropertyGet ||
               method.MethodKind == MethodKind.PropertySet;
    }
}
