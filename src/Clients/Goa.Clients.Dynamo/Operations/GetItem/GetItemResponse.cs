using System.Text.Json.Serialization;
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
    [JsonPropertyName("Item")]
    public DynamoRecord? Item { get; set; }

    /// <summary>
    /// The capacity units consumed by the GetItem operation.
    /// </summary>
    [JsonPropertyName("ConsumedCapacityUnits")]
    public double? ConsumedCapacityUnits { get; set; }

    /// <summary>
    /// The capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("ConsumedCapacity")]
    public ConsumedCapacity? ConsumedCapacity { get; set; }
}
