using Goa.Clients.Dynamo.Enums;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Request for batch writing items to DynamoDB.
/// </summary>
public class BatchWriteItemRequest
{
    /// <summary>
    /// A map of one or more table names and, for each table, a list of operations to be performed.
    /// </summary>
    public Dictionary<string, List<BatchWriteRequestItem>> RequestItems { get; set; } = new();

    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;

    /// <summary>
    /// Determines whether item collection metrics are returned.
    /// </summary>
    public ReturnItemCollectionMetrics ReturnItemCollectionMetrics { get; set; } = ReturnItemCollectionMetrics.NONE;
}