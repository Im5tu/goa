using Goa.Clients.Dynamo.Models;
using System.Text.Json.Serialization;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Represents the DynamoDB-specific data within a stream record
/// </summary>
public class StreamRecord
{
    /// <summary>
    /// Gets or sets the approximate date and time when the stream record was created
    /// </summary>
    [JsonPropertyName("ApproximateCreationDateTime")]
    public DateTime ApproximateCreationDateTime { get; set; }

    /// <summary>
    /// Gets or sets the primary key attributes of the modified item
    /// </summary>
    [JsonPropertyName("Keys")]
    public DynamoRecord? Keys { get; set; }

    /// <summary>
    /// Gets or sets the item after modification (for INSERT and MODIFY events)
    /// </summary>
    [JsonPropertyName("NewImage")]
    public DynamoRecord? NewImage { get; set; }

    /// <summary>
    /// Gets or sets the item before modification (for MODIFY and REMOVE events)
    /// </summary>
    [JsonPropertyName("OldImage")]
    public DynamoRecord? OldImage { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the stream record
    /// </summary>
    [JsonPropertyName("SequenceNumber")]
    public string? SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the size of the stream record in bytes
    /// </summary>
    [JsonPropertyName("SizeBytes")]
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the type of data from the modified DynamoDB item that was captured in this stream record
    /// </summary>
    [JsonPropertyName("StreamViewType")]
    public StreamViewType StreamViewType { get; set; }
}
