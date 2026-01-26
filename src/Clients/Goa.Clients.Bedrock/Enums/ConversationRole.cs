using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Enums;

/// <summary>
/// The role of the entity in a conversation message.
/// </summary>
[JsonConverter(typeof(ConversationRoleConverter))]
public enum ConversationRole
{
    /// <summary>
    /// The message is from the user.
    /// </summary>
    User,

    /// <summary>
    /// The message is from the assistant.
    /// </summary>
    Assistant
}
