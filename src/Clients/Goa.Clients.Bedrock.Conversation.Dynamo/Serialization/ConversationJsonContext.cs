using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Bedrock.Conversation.Dynamo.Serialization;

/// <summary>
/// AOT-safe JSON serialization context for conversation store operations.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, AttributeValue>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ConversationJsonContext : JsonSerializerContext
{
}