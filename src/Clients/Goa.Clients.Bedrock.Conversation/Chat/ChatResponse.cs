using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Represents a response from a chat message send operation.
/// </summary>
public sealed class ChatResponse
{
    /// <summary>
    /// Gets the text content of the response.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Gets the content blocks in the response.
    /// </summary>
    public IReadOnlyList<ContentBlock> Content { get; init; } = [];

    /// <summary>
    /// Gets the reason the model stopped generating.
    /// </summary>
    public StopReason StopReason { get; init; }

    /// <summary>
    /// Gets the token usage for this response.
    /// </summary>
    public TokenUsage Usage { get; init; } = new();

    /// <summary>
    /// Gets the list of tools that were executed during this response.
    /// </summary>
    public IReadOnlyList<ToolExecution> ToolsExecuted { get; init; } = [];

    /// <summary>
    /// Gets the extracted tags from the response content.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ExtractedTags { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>();
}
