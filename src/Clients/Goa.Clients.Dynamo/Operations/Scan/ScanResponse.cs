using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Scan;

/// <summary>
/// Response for Scan operations with pagination support.
/// </summary>
public sealed class ScanResponse
{
    /// <summary>
    /// An array of item attributes that match the scan criteria.
    /// </summary>
    [JsonPropertyName("Items")]
    public List<DynamoRecord> Items { get; set; } = new();

    /// <summary>
    /// The primary key of the item where the operation stopped, inclusive of the previous result set.
    /// Use this value to start a new operation, excluding this value in the new request.
    /// </summary>
    [JsonPropertyName("LastEvaluatedKey")]
    public Dictionary<string, AttributeValue>? LastEvaluatedKey { get; set; }

    /// <summary>
    /// The number of items in the response.
    /// </summary>
    [JsonIgnore]
    public int Count => Items.Count;

    /// <summary>
    /// Gets a value indicating whether there are more results available to retrieve.
    /// </summary>
    [JsonIgnore]
    public bool HasMoreResults => LastEvaluatedKey?.Count > 0;

    /// <summary>
    /// The number of items evaluated, before any ScanFilter is applied.
    /// </summary>
    [JsonPropertyName("ScannedCount")]
    public int ScannedCount { get; set; }

    /// <summary>
    /// The capacity units consumed by the Scan operation.
    /// </summary>
    [JsonPropertyName("ConsumedCapacityUnits")]
    public double? ConsumedCapacityUnits { get; set; }

    /// <summary>
    /// The capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("ConsumedCapacity")]
    public ConsumedCapacity? ConsumedCapacity { get; set; }
}
