using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.ReceiveMessage;

/// <summary>
/// Request for the ReceiveMessage operation.
/// </summary>
public sealed class ReceiveMessageRequest
{
    /// <summary>
    /// The URL of the Amazon SQS queue from which messages are received.
    /// </summary>
    [JsonPropertyName("QueueUrl")]
    public required string QueueUrl { get; set; }

    /// <summary>
    /// The maximum number of messages to return (1-10).
    /// </summary>
    [JsonPropertyName("MaxNumberOfMessages")]
    public int? MaxNumberOfMessages { get; set; }

    /// <summary>
    /// The duration (in seconds) that the received messages are hidden from subsequent retrieve requests.
    /// </summary>
    [JsonPropertyName("VisibilityTimeout")]
    public int? VisibilityTimeout { get; set; }

    /// <summary>
    /// The duration (in seconds) for which the call waits for a message to arrive in the queue before returning.
    /// </summary>
    [JsonPropertyName("WaitTimeSeconds")]
    public int? WaitTimeSeconds { get; set; }

    /// <summary>
    /// A list of attributes that need to be returned along with each message.
    /// </summary>
    [JsonPropertyName("AttributeNames")]
    public List<string>? AttributeNames { get; set; }

    /// <summary>
    /// The name of the message attribute.
    /// </summary>
    [JsonPropertyName("MessageAttributeNames")]
    public List<string>? MessageAttributeNames { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO queues. The token used for deduplication of ReceiveMessage calls.
    /// </summary>
    [JsonPropertyName("ReceiveRequestAttemptId")]
    public string? ReceiveRequestAttemptId { get; set; }
}