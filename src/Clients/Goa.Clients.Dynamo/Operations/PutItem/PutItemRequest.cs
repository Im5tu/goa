using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.PutItem;

/// <summary>
/// Request for putting an item into DynamoDB.
/// </summary>
public class PutItemRequest
{
    /// <summary>
    /// The name of the table to contain the item.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// A map of attribute names to AttributeValue objects representing the item to put.
    /// </summary>
    public Dictionary<string, AttributeValue> Item { get; set; } = new();

    /// <summary>
    /// A condition that must be satisfied in order for a conditional PutItem operation to succeed.
    /// </summary>
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// One or more values that can be substituted in an expression.
    /// </summary>
    public Dictionary<string, AttributeValue>? ExpressionAttributeValues { get; set; }

    /// <summary>
    /// One or more substitution tokens for attribute names in an expression.
    /// </summary>
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }

    /// <summary>
    /// Use ReturnValues if you want to get the item attributes as they appeared before they were deleted.
    /// </summary>
    public ReturnValues ReturnValues { get; set; } = ReturnValues.NONE;

    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;

    /// <summary>
    /// Determines whether item collection metrics are returned.
    /// </summary>
    public ReturnItemCollectionMetrics ReturnItemCollectionMetrics { get; set; } = ReturnItemCollectionMetrics.NONE;

    /// <summary>
    /// Specifies how to return attribute values when a conditional check fails.
    /// Use ALL_OLD to return all attributes of the item as they appeared before the operation.
    /// </summary>
    public ReturnValuesOnConditionCheckFailure ReturnValuesOnConditionCheckFailure { get; set; } = ReturnValuesOnConditionCheckFailure.NONE;
}
