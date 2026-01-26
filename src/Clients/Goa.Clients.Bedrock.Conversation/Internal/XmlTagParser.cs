using System.Text.RegularExpressions;

namespace Goa.Clients.Bedrock.Conversation.Internal;

/// <summary>
/// Parses XML-style tags from text and extracts their contents.
/// </summary>
internal static partial class XmlTagParser
{
    /// <summary>
    /// Parses XML-style tags from text, extracting tag contents and returning cleaned text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>A tuple containing the cleaned text (with tags removed) and a dictionary of tag names to their contents.</returns>
    /// <example>
    /// Input: "&lt;thinking&gt;a&lt;/thinking&gt;&lt;thinking&gt;b&lt;/thinking&gt;&lt;reasoning&gt;c&lt;/reasoning&gt; response"
    /// Output: ("response", { "thinking": ["a", "b"], "reasoning": ["c"] })
    /// </example>
    public static (string CleanedText, IReadOnlyDictionary<string, IReadOnlyList<string>> Tags) Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (string.Empty, new Dictionary<string, IReadOnlyList<string>>());
        }

        var tags = new Dictionary<string, List<string>>();
        var matches = XmlTagRegex().Matches(text);

        foreach (Match match in matches)
        {
            var tagName = match.Groups[1].Value;
            var tagContent = match.Groups[2].Value.Trim();

            if (!tags.TryGetValue(tagName, out var contentList))
            {
                contentList = [];
                tags[tagName] = contentList;
            }

            contentList.Add(tagContent);
        }

        var cleanedText = XmlTagRegex().Replace(text, string.Empty).Trim();

        // Convert to read-only dictionary
        var result = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var kvp in tags)
        {
            result[kvp.Key] = kvp.Value;
        }

        return (cleanedText, result);
    }

    /// <summary>
    /// Regex pattern to match XML-style tags with their contents.
    /// </summary>
    [GeneratedRegex(@"<(\w+)>(.*?)</\1>", RegexOptions.Singleline)]
    private static partial Regex XmlTagRegex();
}
