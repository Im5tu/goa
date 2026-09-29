using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;

/// <summary>
/// Represents an individual entry within a ChangeMessageVisibilityBatch request.
/// </summary>
public sealed class ChangeMessageVisibilityBatchRequestEntry
{
    /// <summary>
    /// An identifier for this entry in the batch. Used to communicate the result.
    /// </summary>
    [JsonPropertyName("Id")]
    public required string Id { get; set; }

    /// <summary>
    /// The receipt handle associated with the message whose visibility timeout is changed.
    /// </summary>
    [JsonPropertyName("ReceiptHandle")]
    public required string ReceiptHandle { get; set; }

    /// <summary>
    /// The new value for the message's visibility timeout, in seconds (0 to 43200).
    /// </summary>
    [JsonPropertyName("VisibilityTimeout")]
    public required int VisibilityTimeout { get; set; }
}
