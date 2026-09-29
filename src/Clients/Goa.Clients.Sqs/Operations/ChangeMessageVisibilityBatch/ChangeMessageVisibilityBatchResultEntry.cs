using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;

/// <summary>
/// Represents an entry whose visibility timeout was successfully changed in a batch.
/// </summary>
public sealed class ChangeMessageVisibilityBatchResultEntry
{
    /// <summary>
    /// The Id of the entry in the batch request.
    /// </summary>
    [JsonPropertyName("Id")]
    public string? Id { get; set; }
}
