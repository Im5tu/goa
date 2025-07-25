using System.Text.Json.Serialization;

namespace Goa.Clients.EventBridge.Models;

/// <summary>
/// Represents the result of processing an event entry in a PutEvents response.
/// </summary>
public sealed class EventResultEntry
{
    /// <summary>
    /// The ID of the event if it was successfully processed.
    /// </summary>
    [JsonPropertyName("EventId")]
    public string? EventId { get; set; }

    /// <summary>
    /// The error code if the event failed to process.
    /// </summary>
    [JsonPropertyName("ErrorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// The error message if the event failed to process.
    /// </summary>
    [JsonPropertyName("ErrorMessage")]
    public string? ErrorMessage { get; set; }
}