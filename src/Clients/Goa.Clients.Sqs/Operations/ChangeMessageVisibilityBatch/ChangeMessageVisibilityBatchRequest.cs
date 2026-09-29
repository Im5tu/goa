using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;

/// <summary>
/// Request for the ChangeMessageVisibilityBatch operation.
/// </summary>
public sealed class ChangeMessageVisibilityBatchRequest
{
    /// <summary>
    /// The URL of the Amazon SQS queue whose messages' visibility is changed.
    /// </summary>
    [JsonPropertyName("QueueUrl")]
    public required string QueueUrl { get; set; }

    /// <summary>
    /// A list of ChangeMessageVisibilityBatchRequestEntry items (max 10 per batch).
    /// </summary>
    [JsonPropertyName("Entries")]
    public required List<ChangeMessageVisibilityBatchRequestEntry> Entries { get; set; }
}
