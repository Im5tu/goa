using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Put request within a batch write operation.
/// </summary>
public sealed class PutRequest
{
    /// <summary>
    /// A map of attribute names to AttributeValue objects representing the item to be put.
    /// </summary>
    [JsonPropertyName("Item")]
    public Dictionary<string, AttributeValue> Item { get; set; } = new();
}
