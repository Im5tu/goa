using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.ChangeMessageVisibility;

/// <summary>
/// Request for the ChangeMessageVisibility operation.
/// </summary>
public sealed class ChangeMessageVisibilityRequest
{
    /// <summary>
    /// The URL of the Amazon SQS queue whose message's visibility is changed.
    /// </summary>
    [JsonPropertyName("QueueUrl")]
    public required string QueueUrl { get; set; }

    /// <summary>
    /// The receipt handle associated with the message whose visibility timeout is changed.
    /// </summary>
    [JsonPropertyName("ReceiptHandle")]
    public required string ReceiptHandle { get; set; }

    /// <summary>
    /// The new value for the message's visibility timeout, in seconds (0 to 43200).
    /// A value of 0 makes the message immediately visible to other consumers.
    /// </summary>
    [JsonPropertyName("VisibilityTimeout")]
    public required int VisibilityTimeout { get; set; }
}
