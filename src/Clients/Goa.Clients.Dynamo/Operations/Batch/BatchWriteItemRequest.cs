using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Enums;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Request for batch writing items to DynamoDB.
/// </summary>
public sealed class BatchWriteItemRequest
{
    /// <summary>
    /// A map of one or more table names and, for each table, a list of operations to be performed.
    /// </summary>
    [JsonPropertyName("RequestItems")]
    public Dictionary<string, List<BatchWriteRequestItem>> RequestItems { get; set; } = new();

    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    [JsonPropertyName("ReturnConsumedCapacity")]
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;

    /// <summary>
    /// Determines whether item collection metrics are returned.
    /// </summary>
    [JsonPropertyName("ReturnItemCollectionMetrics")]
    public ReturnItemCollectionMetrics ReturnItemCollectionMetrics { get; set; } = ReturnItemCollectionMetrics.NONE;
}
