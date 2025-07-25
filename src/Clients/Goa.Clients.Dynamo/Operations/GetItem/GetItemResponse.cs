using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.GetItem;

/// <summary>
/// Response for GetItem operations.
/// </summary>
public class GetItemResponse
{
    /// <summary>
    /// A map of attribute names to AttributeValue objects, as specified by ProjectionExpression.
    /// </summary>
    public DynamoRecord? Item { get; set; }
    
    /// <summary>
    /// The capacity units consumed by the GetItem operation.
    /// </summary>
    public double? ConsumedCapacityUnits { get; set; }
    
    /// <summary>
    /// The capacity units consumed by the operation.
    /// </summary>
    public ConsumedCapacity? ConsumedCapacity { get; set; }
}