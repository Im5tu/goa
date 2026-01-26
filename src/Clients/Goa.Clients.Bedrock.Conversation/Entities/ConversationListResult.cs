namespace Goa.Clients.Bedrock.Conversation.Entities;

/// <summary>
/// Represents the result of listing conversations.
/// </summary>
public class ConversationListResult
{
    /// <summary>
    /// The list of conversations.
    /// </summary>
    public IReadOnlyList<Conversation> Conversations { get; set; } = [];

    /// <summary>
    /// The pagination token to retrieve the next set of conversations.
    /// </summary>
    public string? NextPaginationToken { get; set; }

    /// <summary>
    /// Indicates whether there are more conversations available.
    /// </summary>
    public bool HasMore { get; set; }
}
