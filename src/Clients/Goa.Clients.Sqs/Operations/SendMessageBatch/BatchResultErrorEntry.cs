using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.SendMessageBatch;

/// <summary>
/// Represents a failed message in a batch operation.
/// </summary>
public sealed class BatchResultErrorEntry
{
    /// <summary>
    /// The Id of an entry in a batch request.
    /// </summary>
    [JsonPropertyName("Id")]
    public string? Id { get; set; }

    /// <summary>
    /// An error code representing why the action failed.
    /// </summary>
    [JsonPropertyName("Code")]
    public string? Code { get; set; }

    /// <summary>
    /// A message explaining why the action failed.
    /// </summary>
    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    /// <summary>
    /// Specifies whether the error happened due to the caller (true) or the service (false).
    /// </summary>
    [JsonPropertyName("SenderFault")]
    public bool SenderFault { get; set; }
}
