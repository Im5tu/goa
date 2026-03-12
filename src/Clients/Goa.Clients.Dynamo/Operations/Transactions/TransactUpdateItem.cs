using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Update operation in a transaction.
/// </summary>
public sealed class TransactUpdateItem
{
    /// <summary>
    /// The name of the table containing the item to be updated.
    /// </summary>
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The primary key of the item to be updated.
    /// </summary>
    [JsonPropertyName("Key")]
    public Dictionary<string, AttributeValue> Key { get; set; } = new();

    /// <summary>
    /// An expression that defines one or more attributes to be updated.
    /// </summary>
    [JsonPropertyName("UpdateExpression")]
    public string UpdateExpression { get; set; } = string.Empty;

    /// <summary>
    /// A condition that must be satisfied in order for a conditional UpdateItem to succeed.
    /// </summary>
    [JsonPropertyName("ConditionExpression")]
    public string? ConditionExpression { get; set; }

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
    /// Specifies how to return attribute values when a conditional check fails.
    /// Use ALL_OLD to return all attributes of the item as they appeared before the operation.
    /// </summary>
    [JsonPropertyName("ReturnValuesOnConditionCheckFailure")]
    public ReturnValuesOnConditionCheckFailure ReturnValuesOnConditionCheckFailure { get; set; } = ReturnValuesOnConditionCheckFailure.NONE;
}
