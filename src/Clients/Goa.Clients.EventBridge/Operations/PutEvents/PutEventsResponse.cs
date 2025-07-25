using System.Text.Json.Serialization;
using Goa.Clients.EventBridge.Models;

namespace Goa.Clients.EventBridge.Operations.PutEvents;

/// <summary>
/// Response from the PutEvents operation.
/// </summary>
public sealed class PutEventsResponse
{
    /// <summary>
    /// The number of failed entries.
    /// </summary>
    [JsonPropertyName("FailedEntryCount")]
    public int FailedEntryCount { get; set; }

    /// <summary>
    /// The results for each event entry, in the same order as the request entries.
    /// </summary>
    [JsonPropertyName("Entries")]
    public List<EventResultEntry> Entries { get; set; } = [];
}