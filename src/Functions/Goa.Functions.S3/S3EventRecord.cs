using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents an individual S3 event record
/// </summary>
public class S3EventRecord
{
    /// <summary>
    /// Gets or sets the event version (e.g., "2.1", "2.2", "2.3")
    /// </summary>
    [JsonPropertyName("eventVersion")]
    public string? EventVersion { get; set; }

    /// <summary>
    /// Gets or sets the event source (typically "aws:s3")
    /// </summary>
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; set; }

    /// <summary>
    /// Gets or sets the AWS region where the event occurred
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string? AwsRegion { get; set; }

    /// <summary>
    /// Gets or sets the time when the event occurred in ISO-8601 format
    /// </summary>
    [JsonPropertyName("eventTime")]
    public string? EventTime { get; set; }

    /// <summary>
    /// Gets or sets the event name (e.g., "ObjectCreated:Put", "ObjectRemoved:Delete")
    /// </summary>
    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the user identity that caused the event
    /// </summary>
    [JsonPropertyName("userIdentity")]
    public S3Identity? UserIdentity { get; set; }

    /// <summary>
    /// Gets or sets the request parameters
    /// </summary>
    [JsonPropertyName("requestParameters")]
    public S3RequestParameters? RequestParameters { get; set; }

    /// <summary>
    /// Gets or sets the response elements
    /// </summary>
    [JsonPropertyName("responseElements")]
    public S3ResponseElements? ResponseElements { get; set; }

    /// <summary>
    /// Gets or sets the S3 specific information
    /// </summary>
    [JsonPropertyName("s3")]
    public S3Info? S3 { get; set; }

    /// <summary>
    /// Gets or sets glacier event data (only present for restore events)
    /// </summary>
    [JsonPropertyName("glacierEventData")]
    public GlacierEventData? GlacierEventData { get; set; }

    /// <summary>
    /// Gets or sets replication event data (only present for replication events)
    /// </summary>
    [JsonPropertyName("replicationEventData")]
    public ReplicationEventData? ReplicationEventData { get; set; }

    /// <summary>
    /// Gets or sets lifecycle event data (only present for lifecycle events)
    /// </summary>
    [JsonPropertyName("lifecycleEventData")]
    public LifecycleEventData? LifecycleEventData { get; set; }

    /// <summary>
    /// Gets or sets intelligent tiering event data (only present for intelligent tiering events)
    /// </summary>
    [JsonPropertyName("intelligentTieringEventData")]
    public IntelligentTieringEventData? IntelligentTieringEventData { get; set; }

    private bool _isFailed;

    /// <summary>
    /// Marks this record as failed for processing
    /// </summary>
    public void MarkAsFailed()
    {
        _isFailed = true;
    }

    /// <summary>
    /// Gets whether this record has been marked as failed
    /// </summary>
    public bool IsFailed => _isFailed;
}