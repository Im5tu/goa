namespace Goa.Clients.Bedrock.Conversation.Entities;

/// <summary>
/// Represents a conversation with its associated messages.
/// </summary>
public class ConversationWithMessages
{
    /// <summary>
    /// The conversation.
    /// </summary>
    public required Conversation Conversation { get; set; }

    /// <summary>
    /// The messages in the conversation.
    /// </summary>
    public IReadOnlyList<ConversationMessage> Messages { get; set; } = [];

    /// <summary>
    /// The pagination token to retrieve the next set of messages.
    /// </summary>
    public string? NextPaginationToken { get; set; }

    /// <summary>
    /// Indicates whether there are more messages available.
    /// </summary>
    public bool HasMoreMessages { get; set; }
}
