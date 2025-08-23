using System.Text.RegularExpressions;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Utility methods for consistent naming and normalization.
/// </summary>
public static class NamingHelpers
{
    private static readonly Regex PlaceholderRegex = new(@"<([^>]+)>", RegexOptions.Compiled);
    
    /// <summary>
    /// Normalizes a type name for use in generated code.
    /// </summary>
    public static string NormalizeTypeName(string typeName)
    {
        return typeName.Replace(".", "_").Replace("`", "_");
    }
    
    /// <summary>
    /// Extracts placeholders from a key pattern (e.g., "USER#<Id>" returns ["Id"]).
    /// </summary>
    public static List<string> ExtractPlaceholders(string pattern)
    {
        var matches = PlaceholderRegex.Matches(pattern);
        return matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
    }
    
    /// <summary>
    /// Formats a key pattern by replacing placeholders with actual values.
    /// </summary>
    public static string FormatKeyPattern(string pattern, Dictionary<string, string> replacements)
    {
        var result = pattern;
        foreach (var replacement in replacements)
        {
            result = result.Replace($"<{replacement.Key}>", replacement.Value);
        }
        return result;
    }
    
    /// <summary>
    /// Generates a safe variable name from a property name.
    /// </summary>
    public static string ToVariableName(string propertyName)
    {
        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }
    
    /// <summary>
    /// Checks if a string is a valid C# identifier.
    /// </summary>
    public static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;
            
        if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
            return false;
            
        return identifier.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}