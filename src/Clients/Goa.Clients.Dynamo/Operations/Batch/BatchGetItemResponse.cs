using Goa.Clients.Dynamo.Models;
using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Response for BatchGetItem operations.
/// </summary>
public class BatchGetItemResponse
{
    /// <summary>
    /// A map of table name to a list of items. Each item is represented as a set of attributes.
    /// </summary>
    public Dictionary<string, List<DynamoRecord>> Responses { get; set; } = new();
    
    /// <summary>
    /// A map of tables and their respective keys that were not processed with the current response.
    /// </summary>
    public Dictionary<string, BatchGetRequestItem>? UnprocessedKeys { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether there are unprocessed keys that need to be retried.
    /// </summary>
    [JsonIgnore]
    public bool HasUnprocessedKeys => UnprocessedKeys?.Count > 0;
    
    /// <summary>
    /// The read capacity units consumed by the BatchGetItem operation.
    /// </summary>
    public List<ConsumedCapacity>? ConsumedCapacity { get; set; }
}