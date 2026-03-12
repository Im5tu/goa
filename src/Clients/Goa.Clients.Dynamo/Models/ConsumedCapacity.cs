using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Represents consumed capacity information for DynamoDB operations.
/// </summary>
public sealed class ConsumedCapacity
{
    /// <summary>
    /// The name of the table that was affected by the operation.
    /// </summary>
    [JsonPropertyName("TableName")]
    public string? TableName { get; set; }

    /// <summary>
    /// The total number of capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("CapacityUnits")]
    public double? CapacityUnits { get; set; }

    /// <summary>
    /// The total number of read capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("ReadCapacityUnits")]
    public double? ReadCapacityUnits { get; set; }

    /// <summary>
    /// The total number of write capacity units consumed by the operation.
    /// </summary>
    [JsonPropertyName("WriteCapacityUnits")]
    public double? WriteCapacityUnits { get; set; }

    /// <summary>
    /// The capacity consumed by the global secondary indexes affected by the operation.
    /// </summary>
    [JsonPropertyName("GlobalSecondaryIndexes")]
    public Dictionary<string, CapacityDetail>? GlobalSecondaryIndexes { get; set; }

    /// <summary>
    /// The capacity consumed by the local secondary indexes affected by the operation.
    /// </summary>
    [JsonPropertyName("LocalSecondaryIndexes")]
    public Dictionary<string, CapacityDetail>? LocalSecondaryIndexes { get; set; }

    /// <summary>
    /// The capacity consumed by the table itself.
    /// </summary>
    [JsonPropertyName("Table")]
    public CapacityDetail? Table { get; set; }
}
