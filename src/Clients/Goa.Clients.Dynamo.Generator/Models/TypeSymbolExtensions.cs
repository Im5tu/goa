using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Models;

/// <summary>
/// Helper extensions for inspecting Roslyn type symbols.
/// </summary>
internal static class TypeSymbolExtensions
{
    /// <summary>
    /// Determines whether the given type is System.DateTime or System.DateTimeOffset
    /// using robust symbol identity rather than brittle name comparisons.
    /// </summary>
    public static bool IsDateTimeOrDateTimeOffset(this ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_DateTime)
            return true;

        // DateTimeOffset has no SpecialType, so identify it by its fully-qualified name.
        return type.Name == nameof(DateTimeOffset)
               && type.ContainingNamespace?.ToDisplayString() == "System";
    }
}
