using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Delete operation in a transaction.
/// </summary>
public class TransactDeleteItem
{
    /// <summary>
    /// The name of the table from which to delete the item.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// The primary key of the item to be deleted.
    /// </summary>
    public Dictionary<string, AttributeValue> Key { get; set; } = new();
    
    /// <summary>
    /// A condition that must be satisfied in order for a conditional DeleteItem to succeed.
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