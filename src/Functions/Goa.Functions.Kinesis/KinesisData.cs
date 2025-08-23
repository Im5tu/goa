using System.Text.Json.Serialization;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Represents the Kinesis-specific data within a Kinesis record
/// </summary>
public class KinesisData
{
    /// <summary>
    /// Gets or sets the Kinesis schema version
    /// </summary>
    [JsonPropertyName("kinesisSchemaVersion")]
    public string? KinesisSchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the partition key used to place the record in a specific shard
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets the sequence number assigned by Kinesis to the record
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public string? SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded data payload
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the approximate time when the record was added to the stream
    /// </summary>
    [JsonPropertyName("approximateArrivalTimestamp")]
    public double? ApproximateArrivalTimestamp { get; set; }
}