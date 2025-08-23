using System.Text.RegularExpressions;

namespace Goa.Clients.Dynamo.Generator.Tests.Helpers;

/// <summary>
/// Utilities for comparing generated code with expected output.
/// </summary>
public static class CodeComparisonUtilities
{
    /// <summary>
    /// Normalizes C# code for comparison by removing extra whitespace, normalizing line endings, and formatting consistently.
    /// </summary>
    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return string.Empty;
            
        // Normalize line endings to \n
        code = code.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Remove leading/trailing whitespace
        code = code.Trim();
        
        // Normalize multiple spaces to single space, but preserve indentation
        var lines = code.Split('\n');
        var normalizedLines = new List<string>();
        
        foreach (var line in lines)
        {
            // Preserve leading whitespace (indentation), but normalize internal spacing
            var trimmedLine = line.TrimEnd();
            if (string.IsNullOrEmpty(trimmedLine))
            {
                normalizedLines.Add(string.Empty);
                continue;
            }
            
            // Get leading whitespace
            var leadingWhitespace = GetLeadingWhitespace(trimmedLine);
            var contentWithoutLeading = trimmedLine.Substring(leadingWhitespace.Length);
            
            // Normalize internal spacing - replace multiple spaces/tabs with single space
            contentWithoutLeading = Regex.Replace(contentWithoutLeading, @"\s+", " ");
            
            normalizedLines.Add(leadingWhitespace + contentWithoutLeading);
        }
        
        return string.Join("\n", normalizedLines);
    }
    
    /// <summary>
    /// Compares two pieces of C# code after normalization.
    /// </summary>
    public static bool AreEquivalent(string expected, string actual)
    {
        var normalizedExpected = NormalizeCode(expected);
        var normalizedActual = NormalizeCode(actual);
        
        return string.Equals(normalizedExpected, normalizedActual, StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Compares two pieces of code and returns a detailed difference report.
    /// </summary>
    public static CodeComparisonResult Compare(string expected, string actual, string? context = null)
    {
        var normalizedExpected = NormalizeCode(expected);
        var normalizedActual = NormalizeCode(actual);
        
        var isEqual = string.Equals(normalizedExpected, normalizedActual, StringComparison.Ordinal);
        
        if (isEqual)
        {
            return new CodeComparisonResult
            {
                IsEqual = true,
                Context = context
            };
        }
        
        // Find differences
        var expectedLines = normalizedExpected.Split('\n');
        var actualLines = normalizedActual.Split('\n');
        var differences = new List<string>();
        
        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);
        
        for (int i = 0; i < maxLines; i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "<missing>";
            var actualLine = i < actualLines.Length ? actualLines[i] : "<missing>";
            
            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                differences.Add($"Line {i + 1}:");
                differences.Add($"  Expected: {expectedLine}");
                differences.Add($"  Actual:   {actualLine}");
            }
        }
        
        return new CodeComparisonResult
        {
            IsEqual = false,
            Context = context,
            ExpectedCode = normalizedExpected,
            ActualCode = normalizedActual,
            Differences = differences
        };
    }
    
    /// <summary>
    /// Checks if the generated code contains specific patterns.
    /// </summary>
    public static bool Contains(string code, string pattern, bool ignoreWhitespace = true)
    {
        if (ignoreWhitespace)
        {
            code = Regex.Replace(NormalizeCode(code), @"\s+", " ");
            pattern = Regex.Replace(pattern, @"\s+", " ");
        }
        
        return code.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Checks if the generated code matches a regex pattern.
    /// </summary>
    public static bool MatchesPattern(string code, string regexPattern, RegexOptions options = RegexOptions.None)
    {
        var normalizedCode = NormalizeCode(code);
        return Regex.IsMatch(normalizedCode, regexPattern, options);
    }
    
    /// <summary>
    /// Extracts all matches of a regex pattern from the code.
    /// </summary>
    public static List<string> ExtractPatternMatches(string code, string regexPattern, RegexOptions options = RegexOptions.None)
    {
        var normalizedCode = NormalizeCode(code);
        var matches = Regex.Matches(normalizedCode, regexPattern, options);
        return matches.Cast<Match>().Select(m => m.Value).ToList();
    }
    
    /// <summary>
    /// Validates that the generated code compiles (basic syntax check).
    /// </summary>
    public static List<string> ValidateSyntax(string code)
    {
        var errors = new List<string>();
        
        // Basic syntax validation checks
        if (!HasMatchingBraces(code))
            errors.Add("Unmatched braces detected");
            
        if (!HasMatchingParentheses(code))
            errors.Add("Unmatched parentheses detected");
            
        if (!HasMatchingQuotes(code))
            errors.Add("Unmatched quotes detected");
            
        // Check for common syntax issues
        if (code.Contains(";;"))
            errors.Add("Double semicolons detected");
            
        return errors;
    }
    
    private static string GetLeadingWhitespace(string line)
    {
        var match = Regex.Match(line, @"^(\s*)");
        return match.Groups[1].Value;
    }
    
    private static bool HasMatchingBraces(string code)
    {
        var openCount = code.Count(c => c == '{');
        var closeCount = code.Count(c => c == '}');
        return openCount == closeCount;
    }
    
    private static bool HasMatchingParentheses(string code)
    {
        var openCount = code.Count(c => c == '(');
        var closeCount = code.Count(c => c == ')');
        return openCount == closeCount;
    }
    
    private static bool HasMatchingQuotes(string code)
    {
        // Simple quote matching - doesn't handle escaped quotes perfectly
        var singleQuoteCount = code.Count(c => c == '\'');
        var doubleQuoteCount = code.Count(c => c == '"');
        return singleQuoteCount % 2 == 0 && doubleQuoteCount % 2 == 0;
    }
}

/// <summary>
/// Result of comparing two pieces of code.
/// </summary>
public class CodeComparisonResult
{
    public bool IsEqual { get; set; }
    public string? Context { get; set; }
    public string? ExpectedCode { get; set; }
    public string? ActualCode { get; set; }
    public List<string> Differences { get; set; } = new();
    
    public string GetDifferenceReport()
    {
        var report = new List<string>();
        
        if (!string.IsNullOrEmpty(Context))
            report.Add($"Context: {Context}");
            
        if (IsEqual)
        {
            report.Add("✅ Code matches expected output");
        }
        else
        {
            report.Add("❌ Code does not match expected output");
            report.Add("");
            report.Add("Differences:");
            report.AddRange(Differences);
            
            if (!string.IsNullOrEmpty(ExpectedCode) && !string.IsNullOrEmpty(ActualCode))
            {
                report.Add("");
                report.Add("Expected:");
                report.Add(ExpectedCode);
                report.Add("");
                report.Add("Actual:");
                report.Add(ActualCode);
            }
        }
        
        return string.Join("\n", report);
    }
}