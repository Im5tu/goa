using System.Text.Json.Serialization;

namespace Goa.Clients.EventBridge.Models;

/// <summary>
/// Represents an event entry for EventBridge PutEvents operation.
/// </summary>
public sealed class EventEntry
{
    /// <summary>
    /// The source of the event.
    /// </summary>
    [JsonPropertyName("Source")]
    public string? Source { get; set; }

    /// <summary>
    /// The detail type of the event.
    /// </summary>
    [JsonPropertyName("DetailType")]
    public string? DetailType { get; set; }

    /// <summary>
    /// A JSON string containing the event details.
    /// </summary>
    [JsonPropertyName("Detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// The timestamp of the event.
    /// </summary>
    [JsonPropertyName("Time")]
    public DateTime? Time { get; set; }

    /// <summary>
    /// AWS resources that the event primarily concerns.
    /// </summary>
    [JsonPropertyName("Resources")]
    public List<string>? Resources { get; set; }

    /// <summary>
    /// The name or ARN of the event bus to receive the event.
    /// </summary>
    [JsonPropertyName("EventBusName")]
    public string? EventBusName { get; set; }

    /// <summary>
    /// An identifier for the trace associated with the event.
    /// </summary>
    [JsonPropertyName("TraceHeader")]
    public string? TraceHeader { get; set; }
}