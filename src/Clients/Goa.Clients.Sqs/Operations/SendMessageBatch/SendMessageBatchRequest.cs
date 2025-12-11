using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.SendMessageBatch;

/// <summary>
/// Request for the SendMessageBatch operation.
/// </summary>
public sealed class SendMessageBatchRequest
{
    /// <summary>
    /// The URL of the Amazon SQS queue to which batched messages are sent.
    /// </summary>
    [JsonPropertyName("QueueUrl")]
    public required string QueueUrl { get; set; }

    /// <summary>
    /// A list of SendMessageBatchRequestEntry items (max 10 per batch).
    /// </summary>
    [JsonPropertyName("Entries")]
    public required List<SendMessageBatchRequestEntry> Entries { get; set; }
}
