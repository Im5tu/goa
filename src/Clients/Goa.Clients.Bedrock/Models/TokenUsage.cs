namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// Token usage information for a conversation request.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// The number of tokens in the input.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// The number of tokens in the output.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// The total number of tokens used.
    /// </summary>
    public int TotalTokens { get; set; }
}
