using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Represents a transaction write operation.
/// </summary>
public class TransactWriteOperation
{
    /// <summary>
    /// The type of operation to perform (Put, Update, Delete, or ConditionCheck).
    /// </summary>
    [JsonPropertyName("OperationType")]
    public TransactOperationType OperationType { get; set; }

    /// <summary>
    /// The name of the table containing the item.
    /// </summary>
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The primary key of the item to operate on.
    /// </summary>
    [JsonPropertyName("Key")]
    public Dictionary<string, AttributeValue>? Key { get; set; }

    /// <summary>
    /// A map of attribute names to AttributeValue objects (for Put operations).
    /// </summary>
    [JsonPropertyName("Item")]
    public Dictionary<string, AttributeValue>? Item { get; set; }

    /// <summary>
    /// A condition that must be satisfied in order for the operation to succeed.
    /// </summary>
    [JsonPropertyName("ConditionExpression")]
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// An expression that defines one or more attributes to be updated (for Update operations).
    /// </summary>
    [JsonPropertyName("UpdateExpression")]
    public string? UpdateExpression { get; set; }

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
}
