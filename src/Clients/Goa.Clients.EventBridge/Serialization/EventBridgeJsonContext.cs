using System.Text.Json.Serialization;
using Goa.Clients.EventBridge.Models;
using Goa.Clients.EventBridge.Operations.PutEvents;

namespace Goa.Clients.EventBridge.Serialization;

/// <summary>
/// JSON serialization context for EventBridge operations, optimized for AOT compilation.
/// </summary>
[JsonSerializable(typeof(PutEventsRequest))]
[JsonSerializable(typeof(PutEventsResponse))]
[JsonSerializable(typeof(EventEntry))]
[JsonSerializable(typeof(EventResultEntry))]
[JsonSerializable(typeof(List<EventEntry>))]
[JsonSerializable(typeof(List<EventResultEntry>))]
[JsonSerializable(typeof(List<string>))]
internal partial class EventBridgeJsonContext : JsonSerializerContext
{
}