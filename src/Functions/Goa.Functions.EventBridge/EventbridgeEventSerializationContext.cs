using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.EventBridge;

/// <summary>
/// JSON serialization context for EventBridge events with source generation support
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.General, UseStringEnumConverter = true)]
[JsonSerializable(typeof(EventbridgeEvent))]
public partial class EventbridgeEventSerializationContext : JsonSerializerContext
{
}