using System.Text.Json.Serialization;
using Goa.Clients.Sqs.Operations.SendMessageBatch;

namespace Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;

/// <summary>
/// Response from the ChangeMessageVisibilityBatch operation.
/// </summary>
public sealed class ChangeMessageVisibilityBatchResponse
{
    /// <summary>
    /// A list of ChangeMessageVisibilityBatchResultEntry items for entries that were changed.
    /// </summary>
    [JsonPropertyName("Successful")]
    public List<ChangeMessageVisibilityBatchResultEntry>? Successful { get; set; }

    /// <summary>
    /// A list of BatchResultErrorEntry items for entries that could not be changed.
    /// </summary>
    [JsonPropertyName("Failed")]
    public List<BatchResultErrorEntry>? Failed { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are any failed entries.
    /// </summary>
    [JsonIgnore]
    public bool HasFailures => Failed?.Count > 0;

    /// <summary>
    /// Gets a value indicating whether all entries were successfully changed.
    /// </summary>
    [JsonIgnore]
    public bool AllSuccessful => Failed is null || Failed.Count == 0;
}
