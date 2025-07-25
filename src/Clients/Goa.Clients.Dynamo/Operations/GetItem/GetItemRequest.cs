using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.GetItem;

/// <summary>
/// Request for getting an item from DynamoDB.
/// </summary>
public class GetItemRequest
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
    /// Determines the read consistency model. If set to true, strongly consistent reads are used; 
    /// otherwise, eventually consistent reads are used.
    /// </summary>
    public bool ConsistentRead { get; set; } = false;
    
    /// <summary>
    /// A string that identifies one or more attributes to retrieve from the table. 
    /// These attributes can include scalars, sets, or elements of a JSON document.
    /// </summary>
    public string? ProjectionExpression { get; set; }
    
    /// <summary>
    /// One or more substitution tokens for attribute names in an expression.
    /// </summary>
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }
    
    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;
}