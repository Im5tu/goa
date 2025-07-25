using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Result wrapper for BatchGet operations.
/// </summary>
public class BatchGetResult
{
    /// <summary>
    /// The items retrieved by the BatchGetItem operation.
    /// </summary>
    public List<DynamoRecord> Items { get; set; } = new();
    
    /// <summary>
    /// Keys that were not processed during the BatchGetItem operation.
    /// </summary>
    public List<Dictionary<string, AttributeValue>> UnprocessedKeys { get; set; } = new();
    
    /// <summary>
    /// Gets a value indicating whether there are unprocessed keys that require additional requests.
    /// </summary>
    public bool HasUnprocessedKeys => UnprocessedKeys.Count > 0;
    
    /// <summary>
    /// The number of capacity units consumed by the operation.
    /// </summary>
    public double ConsumedCapacityUnits { get; set; }
}