using Goa.Clients.Bedrock.Enums;

namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// A message in a conversation with a model.
/// </summary>
public class Message
{
    /// <summary>
    /// The role of the entity sending the message.
    /// </summary>
    public ConversationRole Role { get; set; }

    /// <summary>
    /// The content blocks that comprise the message.
    /// </summary>
    public List<ContentBlock> Content { get; set; } = new();
}
