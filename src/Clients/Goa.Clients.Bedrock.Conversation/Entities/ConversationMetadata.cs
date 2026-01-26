namespace Goa.Clients.Bedrock.Conversation.Entities;

/// <summary>
/// Metadata associated with a conversation.
/// </summary>
public class ConversationMetadata
{
    /// <summary>
    /// The title of the conversation.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Tags associated with the conversation for categorization and filtering.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// The model identifier used for this conversation.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Custom data associated with the conversation.
    /// </summary>
    public Dictionary<string, string> CustomData { get; set; } = [];
}
