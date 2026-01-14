using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.DeleteItem;

/// <summary>
/// Response for DeleteItem operations.
/// </summary>
public class DeleteItemResponse
{
    /// <summary>
    /// A map of attribute names to AttributeValue objects representing the item as it appeared before it was deleted.
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