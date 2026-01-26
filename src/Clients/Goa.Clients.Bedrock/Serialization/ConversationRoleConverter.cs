using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Enums;

namespace Goa.Clients.Bedrock.Serialization;

/// <summary>
/// Custom JSON converter for ConversationRole that serializes to lowercase
/// values as required by the Bedrock API ("user", "assistant").
/// </summary>
public sealed class ConversationRoleConverter : JsonConverter<ConversationRole>
{
    /// <inheritdoc />
    public override ConversationRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.ToLowerInvariant() switch
        {
            "user" => ConversationRole.User,
            "assistant" => ConversationRole.Assistant,
            _ => throw new JsonException($"Unknown ConversationRole: {value}")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConversationRole value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            ConversationRole.User => "user",
            ConversationRole.Assistant => "assistant",
            _ => throw new JsonException($"Unknown ConversationRole: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}
