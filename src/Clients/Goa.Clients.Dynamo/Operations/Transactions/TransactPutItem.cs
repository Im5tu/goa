using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Put operation in a transaction.
/// </summary>
public class TransactPutItem
{
    /// <summary>
    /// The name of the table to contain the item.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// A map of attribute names to AttributeValue objects representing the item.
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
}