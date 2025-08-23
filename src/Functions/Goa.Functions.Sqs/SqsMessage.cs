using System.Text.Json.Serialization;

namespace Goa.Functions.Sqs;

/// <summary>
/// Represents a single SQS message
/// </summary>
public class SqsMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message
    /// </summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the receipt handle for the message (used for deletion)
    /// </summary>
    [JsonPropertyName("receiptHandle")]
    public string? ReceiptHandle { get; set; }

    /// <summary>
    /// Gets or sets the message body content
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the message attributes (system-defined)
    /// </summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the message attributes (user-defined)
    /// </summary>
    [JsonPropertyName("messageAttributes")]
    public Dictionary<string, SqsMessageAttribute>? MessageAttributes { get; set; }

    /// <summary>
    /// Gets or sets the MD5 hash of the message body
    /// </summary>
    [JsonPropertyName("md5OfBody")]
    public string? Md5OfBody { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier (typically "aws:sqs")
    /// </summary>
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; set; }

    /// <summary>
    /// Gets or sets the Amazon Resource Name (ARN) of the SQS queue
    /// </summary>
    [JsonPropertyName("eventSourceARN")]
    public string? EventSourceArn { get; set; }

    /// <summary>
    /// Gets or sets the AWS region where the message originated
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string? AwsRegion { get; set; }

    internal ProcessingType ProcessingType { get; private set; }

    /// <summary>
    /// Marks this particular message as failed in the batch processing
    /// </summary>
    public void MarkAsFailed() => ProcessingType = ProcessingType.Failure;
}