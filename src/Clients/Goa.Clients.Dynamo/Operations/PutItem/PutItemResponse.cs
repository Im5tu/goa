using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.PutItem;

/// <summary>
/// Response for PutItem operations.
/// </summary>
public class PutItemResponse
{
    /// <summary>
    /// The attribute values as they appeared before the PutItem operation, 
    /// but only if ReturnValues is specified as something other than NONE in the request.
    /// </summary>
    public DynamoRecord? Attributes { get; set; }
    
    /// <summary>
    /// The capacity units consumed by the operation.
    /// </summary>
    public ConsumedCapacity? ConsumedCapacity { get; set; }
    
    /// <summary>
    /// Information about item collections, if any, that were affected by the operation.
    /// </summary>
    public Dictionary<string, List<Dictionary<string, AttributeValue>>>? ItemCollectionMetrics { get; set; }
}