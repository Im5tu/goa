using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.SendMessageBatch;

/// <summary>
/// Represents a successfully sent message in a batch.
/// </summary>
public sealed class SendMessageBatchResultEntry
{
    /// <summary>
    /// An identifier for the message in this batch.
    /// </summary>
    [JsonPropertyName("Id")]
    public string? Id { get; set; }

    /// <summary>
    /// An identifier for the message.
    /// </summary>
    [JsonPropertyName("MessageId")]
    public string? MessageId { get; set; }

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
    /// This parameter applies only to FIFO queues. The sequence number.
    /// </summary>
    [JsonPropertyName("SequenceNumber")]
    public string? SequenceNumber { get; set; }
}
