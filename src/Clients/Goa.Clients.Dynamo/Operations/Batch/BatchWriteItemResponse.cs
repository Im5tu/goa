using Goa.Clients.Dynamo.Models;
using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Response for BatchWriteItem operations.
/// </summary>
public class BatchWriteItemResponse
{
    /// <summary>
    /// A map of tables and requests against those tables that were not processed.
    /// </summary>
    public Dictionary<string, List<BatchWriteRequestItem>>? UnprocessedItems { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether there are unprocessed items that need to be retried.
    /// </summary>
    [JsonIgnore]
    public bool HasUnprocessedItems => UnprocessedItems?.Count > 0;
    
    /// <summary>
    /// The write capacity units consumed by the BatchWriteItem operation.
    /// </summary>
    public List<ConsumedCapacity>? ConsumedCapacity { get; set; }
}