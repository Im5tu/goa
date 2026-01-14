using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.UpdateItem;

/// <summary>
/// Response for UpdateItem operations.
/// </summary>
public class UpdateItemResponse
{
    /// <summary>
    /// A map of attribute names to AttributeValue objects representing the item as it appeared before it was updated.
    /// </summary>
    public DynamoRecord? Attributes { get; set; }
    
    /// <summary>
    /// The number of capacity units consumed by the operation.
    /// </summary>
    public double? ConsumedCapacityUnits { get; set; }

    /// <summary>
    /// Information about item collections, if any, that were affected by the operation.
    /// </summary>
    public Dictionary<string, List<ItemCollectionMetrics>>? ItemCollectionMetrics { get; set; }
}