using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Result wrapper for BatchWrite operations.
/// </summary>
public class BatchWriteResult
{
    /// <summary>
    /// Put items that were not processed during the BatchWriteItem operation.
    /// </summary>
    public List<Dictionary<string, AttributeValue>> UnprocessedPutItems { get; set; } = new();
    
    /// <summary>
    /// Delete keys that were not processed during the BatchWriteItem operation.
    /// </summary>
    public List<Dictionary<string, AttributeValue>> UnprocessedDeleteKeys { get; set; } = new();
    
    /// <summary>
    /// Gets a value indicating whether there are unprocessed items that require additional requests.
    /// </summary>
    public bool HasUnprocessedItems => UnprocessedPutItems.Count > 0 || UnprocessedDeleteKeys.Count > 0;
    
    /// <summary>
    /// The number of capacity units consumed by the operation.
    /// </summary>
    public double ConsumedCapacityUnits { get; set; }
}