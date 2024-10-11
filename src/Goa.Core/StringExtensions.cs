namespace Goa.Core;

public static class StringExtensions
{
    /// <summary>
    ///     Compares the string to a target string using the OrdinalIgnoreCase comparison
    /// </summary>
    public static bool EqualsIgnoreCase(this string source, string target) => string.Equals(source, target, StringComparison.OrdinalIgnoreCase);
}
