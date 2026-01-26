using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Conversation.Entities;

/// <summary>
/// Represents a conversation with a Bedrock model.
/// </summary>
public class Conversation
{
    /// <summary>
    /// The unique identifier for the conversation.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Metadata associated with the conversation.
    /// </summary>
    public ConversationMetadata? Metadata { get; set; }

    /// <summary>
    /// The timestamp when the conversation was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the conversation was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The total number of messages in the conversation.
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// The total token usage across all messages in the conversation.
    /// </summary>
    public TokenUsage? TotalTokenUsage { get; set; }
}
