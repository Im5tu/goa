using Goa.Clients.Dynamo.Enums;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Request for batch getting items from DynamoDB.
/// </summary>
public class BatchGetItemRequest
{
    /// <summary>
    /// A map of one or more table names and, for each table, a map that describes one or more items to retrieve from that table.
    /// </summary>
    public Dictionary<string, BatchGetRequestItem> RequestItems { get; set; } = new();
    
    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;
}