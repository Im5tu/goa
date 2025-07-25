using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Get request within a transact get operation.
/// </summary>
public class TransactGetItemRequest
{
    /// <summary>
    /// The name of the table containing the requested item.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// A map of attribute names to AttributeValue objects representing the primary key of the item to retrieve.
    /// </summary>
    public Dictionary<string, AttributeValue> Key { get; set; } = new();
    
    /// <summary>
    /// A string that identifies one or more attributes to retrieve from the table.
    /// </summary>
    public string? ProjectionExpression { get; set; }
    
    /// <summary>
    /// One or more substitution tokens for attribute names in an expression.
    /// </summary>
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }
}