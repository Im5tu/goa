using System.Text.Json.Serialization;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Represents a single Kinesis record within a Kinesis event
/// </summary>
public class KinesisRecord
{
    private ProcessingType _processingType = ProcessingType.Success;

    /// <summary>
    /// Gets or sets the Kinesis-specific data
    /// </summary>
    [JsonPropertyName("kinesis")]
    public KinesisData? Kinesis { get; set; }

    /// <summary>
    /// Gets or sets the event source (always "aws:kinesis")
    /// </summary>
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; set; }

    /// <summary>
    /// Gets or sets the event version
    /// </summary>
    [JsonPropertyName("eventVersion")]
    public string? EventVersion { get; set; }

    /// <summary>
    /// Gets or sets the event ID (combination of shard ID and sequence number)
    /// </summary>
    [JsonPropertyName("eventID")]
    public string? EventId { get; set; }

    /// <summary>
    /// Gets or sets the event name (always "aws:kinesis:record")
    /// </summary>
    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the ARN of the IAM role used to invoke the Lambda function
    /// </summary>
    [JsonPropertyName("invokeIdentityArn")]
    public string? InvokeIdentityArn { get; set; }

    /// <summary>
    /// Gets or sets the AWS region where the event originated
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string? AwsRegion { get; set; }

    /// <summary>
    /// Gets or sets the ARN of the Kinesis stream
    /// </summary>
    [JsonPropertyName("eventSourceARN")]
    public string? EventSourceArn { get; set; }

    /// <summary>
    /// Gets the processing type for this record
    /// </summary>
    [JsonIgnore]
    internal ProcessingType ProcessingType => _processingType;

    /// <summary>
    /// Marks this record as failed for batch failure reporting
    /// </summary>
    public void MarkAsFailed()
    {
        _processingType = ProcessingType.Failure;
    }
}