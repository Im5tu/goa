using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Scan;

/// <summary>
/// Request for scanning items from DynamoDB.
/// </summary>
public sealed class ScanRequest
{
    /// <summary>
    /// The name of the table containing the requested items.
    /// </summary>
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// A string that contains conditions that DynamoDB applies after the Scan operation, but before the data is returned to you.
    /// </summary>
    [JsonPropertyName("FilterExpression")]
    public string? FilterExpression { get; set; }

    /// <summary>
    /// One or more values that can be substituted in an expression.
    /// </summary>
    [JsonPropertyName("ExpressionAttributeValues")]
    public Dictionary<string, AttributeValue>? ExpressionAttributeValues { get; set; }

    /// <summary>
    /// One or more substitution tokens for attribute names in an expression.
    /// </summary>
    [JsonPropertyName("ExpressionAttributeNames")]
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }

    /// <summary>
    /// The maximum number of items to evaluate (not necessarily the number of matching items).
    /// </summary>
    [JsonPropertyName("Limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// The primary key of the first item that this operation will evaluate.
    /// </summary>
    [JsonPropertyName("ExclusiveStartKey")]
    public Dictionary<string, AttributeValue>? ExclusiveStartKey { get; set; }

    /// <summary>
    /// The name of a secondary index to scan. This index can be any local secondary index or global secondary index.
    /// </summary>
    [JsonPropertyName("IndexName")]
    public string? IndexName { get; set; }

    /// <summary>
    /// A string that identifies one or more attributes to retrieve from the specified table or index.
    /// </summary>
    [JsonPropertyName("ProjectionExpression")]
    public string? ProjectionExpression { get; set; }

    /// <summary>
    /// The attributes to be returned in the result.
    /// </summary>
    [JsonPropertyName("Select")]
    public Select Select { get; set; } = Select.ALL_ATTRIBUTES;

    /// <summary>
    /// The total number of segments for a parallel scan request.
    /// </summary>
    [JsonPropertyName("TotalSegments")]
    public int? TotalSegments { get; set; }

    /// <summary>
    /// For a parallel scan request, Segment identifies an individual segment to be scanned by an application worker.
    /// </summary>
    [JsonPropertyName("Segment")]
    public int? Segment { get; set; }

    /// <summary>
    /// Determines the read consistency model. If set to true, strongly consistent reads are used.
    /// </summary>
    [JsonPropertyName("ConsistentRead")]
    public bool ConsistentRead { get; set; } = false;

    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    [JsonPropertyName("ReturnConsumedCapacity")]
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;
}
