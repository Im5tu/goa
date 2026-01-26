using System.Text.Json.Serialization;

namespace Goa.Clients.Bedrock.Operations.CountTokens;

/// <summary>
/// Response from the Bedrock CountTokens API.
/// </summary>
public sealed class CountTokensResponse
{
    /// <summary>
    /// The number of input tokens in the request.
    /// </summary>
    [JsonPropertyName("inputTokens")]
    public int InputTokens { get; init; }
}
