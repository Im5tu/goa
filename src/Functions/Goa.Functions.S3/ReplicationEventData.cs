using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents replication event data for S3 replication events
/// </summary>
public class ReplicationEventData
{
    /// <summary>
    /// Gets or sets the replication rule ID
    /// </summary>
    [JsonPropertyName("replicationRuleId")]
    public string? ReplicationRuleId { get; set; }

    /// <summary>
    /// Gets or sets the destination bucket
    /// </summary>
    [JsonPropertyName("destinationBucket")]
    public string? DestinationBucket { get; set; }

    /// <summary>
    /// Gets or sets the replication status
    /// </summary>
    [JsonPropertyName("s3Operation")]
    public string? S3Operation { get; set; }

    /// <summary>
    /// Gets or sets the request time
    /// </summary>
    [JsonPropertyName("requestTime")]
    public string? RequestTime { get; set; }

    /// <summary>
    /// Gets or sets the failure reason (for failed replications)
    /// </summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the threshold time in minutes
    /// </summary>
    [JsonPropertyName("threshold")]
    public int? Threshold { get; set; }

    /// <summary>
    /// Gets or sets the replication time
    /// </summary>
    [JsonPropertyName("replicationTime")]
    public string? ReplicationTime { get; set; }
}