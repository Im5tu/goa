using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Update operation in a transaction.
/// </summary>
public class TransactUpdateItem
{
    /// <summary>
    /// The name of the table containing the item to be updated.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// The primary key of the item to be updated.
    /// </summary>
    public Dictionary<string, AttributeValue> Key { get; set; } = new();
    
    /// <summary>
    /// An expression that defines one or more attributes to be updated.
    /// </summary>
    public string UpdateExpression { get; set; } = string.Empty;
    
    /// <summary>
    /// A condition that must be satisfied in order for a conditional UpdateItem to succeed.
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
    /// Specifies how to return attribute values when a conditional check fails.
    /// Use ALL_OLD to return all attributes of the item as they appeared before the operation.
    /// </summary>
    public ReturnValuesOnConditionCheckFailure ReturnValuesOnConditionCheckFailure { get; set; } = ReturnValuesOnConditionCheckFailure.NONE;
}