using System.Text.Json.Serialization;
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
    [JsonPropertyName("Attributes")]
    public DynamoRecord? Attributes { get; set; }

    /// <summary>
    /// The capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("ConsumedCapacity")]
    public ConsumedCapacity? ConsumedCapacity { get; set; }

    /// <summary>
    /// Information about item collections, if any, that were affected by the operation.
    /// </summary>
    [JsonPropertyName("ItemCollectionMetrics")]
    public ItemCollectionMetrics? ItemCollectionMetrics { get; set; }
}
