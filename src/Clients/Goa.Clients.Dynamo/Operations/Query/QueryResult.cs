using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Query;

/// <summary>
/// Result wrapper for Query operations with pagination support.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// The items returned by the Query operation.
    /// </summary>
    public List<DynamoRecord> Items { get; set; } = new();
    
    /// <summary>
    /// The primary key of the item where the operation stopped, inclusive of the previous result set.
    /// Use this value to start a new operation, excluding this value in the new request.
    /// </summary>
    public Dictionary<string, AttributeValue>? LastEvaluatedKey { get; set; }
    
    /// <summary>
    /// Gets the number of items in the response.
    /// </summary>
    public int Count => Items.Count;
    
    /// <summary>
    /// Gets a value indicating whether there are more results to retrieve.
    /// </summary>
    public bool HasMoreResults => LastEvaluatedKey?.Count > 0;
    
    /// <summary>
    /// The number of items evaluated, before applying any QueryFilter.
    /// </summary>
    public int ScannedCount { get; set; }
    
    /// <summary>
    /// The number of capacity units consumed by the operation.
    /// </summary>
    public double ConsumedCapacityUnits { get; set; }
}