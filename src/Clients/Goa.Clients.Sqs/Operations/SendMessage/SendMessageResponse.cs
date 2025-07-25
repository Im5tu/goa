using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.SendMessage;

/// <summary>
/// Response from the SendMessage operation.
/// </summary>
public sealed class SendMessageResponse
{
    /// <summary>
    /// An MD5 digest of the non-URL-encoded message body string.
    /// </summary>
    [JsonPropertyName("MD5OfMessageBody")]
    public string? MD5OfMessageBody { get; set; }

    /// <summary>
    /// An MD5 digest of the non-URL-encoded message attributes.
    /// </summary>
    [JsonPropertyName("MD5OfMessageAttributes")]
    public string? MD5OfMessageAttributes { get; set; }

    /// <summary>
    /// An MD5 digest of the non-URL-encoded message system attributes.
    /// </summary>
    [JsonPropertyName("MD5OfMessageSystemAttributes")]
    public string? MD5OfMessageSystemAttributes { get; set; }

    /// <summary>
    /// An attribute containing the MessageId of the message sent to the queue.
    /// </summary>
    [JsonPropertyName("MessageId")]
    public string? MessageId { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO queues. The large, non-consecutive number that Amazon SQS assigns to each message.
    /// </summary>
    [JsonPropertyName("SequenceNumber")]
    public string? SequenceNumber { get; set; }
}