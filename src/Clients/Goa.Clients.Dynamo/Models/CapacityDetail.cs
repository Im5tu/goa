using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Detailed capacity information for DynamoDB operations.
/// </summary>
public sealed class CapacityDetail
{
    /// <summary>
    /// The number of read capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("ReadCapacityUnits")]
    public double? ReadCapacityUnits { get; set; }

    /// <summary>
    /// The number of write capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("WriteCapacityUnits")]
    public double? WriteCapacityUnits { get; set; }

    /// <summary>
    /// The total number of capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("CapacityUnits")]
    public double? CapacityUnits { get; set; }
}
