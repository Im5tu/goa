using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Conversation.Entities;

/// <summary>
/// Represents a message within a conversation.
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// The unique identifier for the message.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The identifier of the conversation this message belongs to.
    /// </summary>
    public required string ConversationId { get; set; }

    /// <summary>
    /// The sequence number of the message within the conversation.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The role of the entity that sent the message.
    /// </summary>
    public ConversationRole Role { get; set; }

    /// <summary>
    /// The message content from the Bedrock models.
    /// </summary>
    public required Message Message { get; set; }

    /// <summary>
    /// Token usage information for this message.
    /// </summary>
    public TokenUsage? TokenUsage { get; set; }

    /// <summary>
    /// The timestamp when the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
