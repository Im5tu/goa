using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Represents a message in the conversation history.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    /// Gets the role of the entity that sent this message.
    /// </summary>
    public ConversationRole Role { get; init; }

    /// <summary>
    /// Gets the content blocks that make up this message.
    /// </summary>
    public IReadOnlyList<ContentBlock> Content { get; init; } = [];

    /// <summary>
    /// Gets the timestamp when this message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the optional token usage associated with this message.
    /// </summary>
    public TokenUsage? TokenUsage { get; init; }
}
