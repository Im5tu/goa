using System.Text.Json.Serialization;
using Goa.Clients.EventBridge.Models;

namespace Goa.Clients.EventBridge.Operations.PutEvents;

/// <summary>
/// Request for the PutEvents operation.
/// </summary>
public sealed class PutEventsRequest
{
    /// <summary>
    /// The event entries to send to EventBridge. Maximum of 10 entries per request.
    /// </summary>
    [JsonPropertyName("Entries")]
    public required List<EventEntry> Entries { get; set; }

    /// <summary>
    /// The URL subdomain of the endpoint for multi-region endpoints.
    /// </summary>
    [JsonPropertyName("EndpointId")]
    public string? EndpointId { get; set; }
}