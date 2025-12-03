using System.Text.Json.Serialization;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Represents a single log event within a CloudWatch Logs subscription filter batch
/// </summary>
public class CloudWatchLogEvent
{
    private ProcessingType _processingType = ProcessingType.Success;

    /// <summary>
    /// Gets or sets the unique identifier for this log event
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the log event was ingested (Unix milliseconds)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the raw log message content
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets any fields extracted by the subscription filter pattern
    /// </summary>
    [JsonPropertyName("extractedFields")]
    public Dictionary<string, string>? ExtractedFields { get; set; }

    /// <summary>
    /// Gets the timestamp as a DateTimeOffset
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset TimestampDateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);

    /// <summary>
    /// Gets the processing type for this log event (internal use)
    /// </summary>
    [JsonIgnore]
    internal ProcessingType ProcessingType => _processingType;

    /// <summary>
    /// Marks this log event as failed for error tracking purposes.
    /// Note: CloudWatch Logs does not support partial batch failures,
    /// but this can be used for logging/metrics purposes.
    /// </summary>
    public void MarkAsFailed() => _processingType = ProcessingType.Failure;
}
