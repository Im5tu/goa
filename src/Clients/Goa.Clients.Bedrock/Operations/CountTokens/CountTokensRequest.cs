using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.Converse;

namespace Goa.Clients.Bedrock.Operations.CountTokens;

/// <summary>
/// Request for the Bedrock CountTokens API.
/// </summary>
public sealed class CountTokensRequest
{
    /// <summary>
    /// The identifier of the model to use for token counting.
    /// </summary>
    [JsonPropertyName("modelId")]
    public required string ModelId { get; init; }

    /// <summary>
    /// The conversation messages to count tokens for.
    /// </summary>
    [JsonPropertyName("messages")]
    public required List<Message> Messages { get; init; }

    /// <summary>
    /// System prompts to include in token counting.
    /// </summary>
    [JsonPropertyName("system")]
    public List<SystemContentBlock>? System { get; init; }

    /// <summary>
    /// Tool configuration to include in token counting.
    /// </summary>
    [JsonPropertyName("toolConfig")]
    public ToolConfiguration? ToolConfig { get; init; }
}
