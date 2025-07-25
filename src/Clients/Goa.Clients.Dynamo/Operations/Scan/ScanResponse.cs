using Goa.Clients.Dynamo.Models;
using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Operations.Scan;

/// <summary>
/// Response for Scan operations with pagination support.
/// </summary>
public class ScanResponse
{
    /// <summary>
    /// An array of item attributes that match the scan criteria.
    /// </summary>
    public List<DynamoRecord> Items { get; set; } = new();
    
    /// <summary>
    /// The primary key of the item where the operation stopped, inclusive of the previous result set.
    /// Use this value to start a new operation, excluding this value in the new request.
    /// </summary>
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
    public int ScannedCount { get; set; }
    
    /// <summary>
    /// The capacity units consumed by the Scan operation.
    /// </summary>
    public double? ConsumedCapacityUnits { get; set; }
    
    /// <summary>
    /// The capacity units consumed by the operation.
    /// </summary>
    public ConsumedCapacity? ConsumedCapacity { get; set; }
}