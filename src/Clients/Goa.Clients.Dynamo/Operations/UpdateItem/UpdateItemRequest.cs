using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.UpdateItem;

/// <summary>
/// Request for updating an item in DynamoDB.
/// </summary>
public class UpdateItemRequest
{
    /// <summary>
    /// The name of the table containing the item to update.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The primary key of the item to be updated.
    /// </summary>
    public Dictionary<string, AttributeValue> Key { get; set; } = new();

    /// <summary>
    /// An expression that defines one or more attributes to be updated, the action to be performed on them, and new values for them.
    /// </summary>
    public string UpdateExpression { get; set; } = string.Empty;

    /// <summary>
    /// A condition that must be satisfied in order for a conditional update to succeed.
    /// </summary>
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// One or more values that can be substituted in an expression.
    /// </summary>
    public Dictionary<string, AttributeValue>? ExpressionAttributeValues { get; set; }= new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// One or more substitution tokens for attribute names in an expression.
    /// </summary>
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }

    /// <summary>
    /// Use ReturnValues if you want to get the item attributes as they appeared before they were updated.
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
}
