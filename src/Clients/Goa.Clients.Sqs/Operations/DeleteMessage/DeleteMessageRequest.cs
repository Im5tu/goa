using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.DeleteMessage;

/// <summary>
/// Request for the DeleteMessage operation.
/// </summary>
public sealed class DeleteMessageRequest
{
    /// <summary>
    /// The URL of the Amazon SQS queue from which messages are deleted.
    /// </summary>
    [JsonPropertyName("QueueUrl")]
    public required string QueueUrl { get; set; }

    /// <summary>
    /// The receipt handle associated with the message to delete.
    /// </summary>
    [JsonPropertyName("ReceiptHandle")]
    public required string ReceiptHandle { get; set; }
}