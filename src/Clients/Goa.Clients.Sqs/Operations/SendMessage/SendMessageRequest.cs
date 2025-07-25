using System.Text.Json.Serialization;
using Goa.Clients.Sqs.Models;

namespace Goa.Clients.Sqs.Operations.SendMessage;

/// <summary>
/// Request for the SendMessage operation.
/// </summary>
public sealed class SendMessageRequest
{
    /// <summary>
    /// The URL of the Amazon SQS queue to which a message is sent.
    /// </summary>
    [JsonPropertyName("QueueUrl")]
    public required string QueueUrl { get; set; }

    /// <summary>
    /// The message to send.
    /// </summary>
    [JsonPropertyName("MessageBody")]
    public required string MessageBody { get; set; }

    /// <summary>
    /// The number of seconds to delay a specific message.
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
    /// This parameter applies only to FIFO queues. The token used for deduplication of sent messages.
    /// </summary>
    [JsonPropertyName("MessageDeduplicationId")]
    public string? MessageDeduplicationId { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO queues. The tag that specifies that a message belongs to a specific message group.
    /// </summary>
    [JsonPropertyName("MessageGroupId")]
    public string? MessageGroupId { get; set; }
}