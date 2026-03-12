using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Delete request within a batch write operation.
/// </summary>
public class DeleteRequest
{
    /// <summary>
    /// The primary key of the item to be deleted.
    /// </summary>
    [JsonPropertyName("Key")]
    public Dictionary<string, AttributeValue> Key { get; set; } = new();
}
