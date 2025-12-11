using System.Text.Json.Serialization;
using Goa.Clients.Sqs.Models;

namespace Goa.Clients.Sqs.Operations.SendMessageBatch;

/// <summary>
/// Represents an individual message entry within a batch send request.
/// </summary>
public sealed class SendMessageBatchRequestEntry
{
    /// <summary>
    /// An identifier for a message in this batch. Used to communicate the result.
    /// </summary>
    [JsonPropertyName("Id")]
    public required string Id { get; set; }

    /// <summary>
    /// The body of the message.
    /// </summary>
    [JsonPropertyName("MessageBody")]
    public required string MessageBody { get; set; }

    /// <summary>
    /// The number of seconds to delay this specific message.
    /// </summary>
    [JsonPropertyName("DelaySeconds")]
    public int? DelaySeconds { get; set; }

    /// <summary>
    /// Each message attribute consists of a Name, Type, and Value.
    /// </summary>
    [JsonPropertyName("MessageAttributes")]
    public Dictionary<string, MessageAttributeValue>? MessageAttributes { get; set; }

    /// <summary>
    /// The message system attributes to set.
    /// </summary>
    [JsonPropertyName("MessageSystemAttributes")]
    public Dictionary<string, MessageAttributeValue>? MessageSystemAttributes { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO queues. The token used for deduplication.
    /// </summary>
    [JsonPropertyName("MessageDeduplicationId")]
    public string? MessageDeduplicationId { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO queues. The tag that specifies the message group.
    /// </summary>
    [JsonPropertyName("MessageGroupId")]
    public string? MessageGroupId { get; set; }
}
