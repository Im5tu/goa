using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Operations.SendMessageBatch;

/// <summary>
/// Response from the SendMessageBatch operation.
/// </summary>
public sealed class SendMessageBatchResponse
{
    /// <summary>
    /// A list of SendMessageBatchResultEntry items for successful messages.
    /// </summary>
    [JsonPropertyName("Successful")]
    public List<SendMessageBatchResultEntry>? Successful { get; set; }

    /// <summary>
    /// A list of BatchResultErrorEntry items for failed messages.
    /// </summary>
    [JsonPropertyName("Failed")]
    public List<BatchResultErrorEntry>? Failed { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are any failed entries.
    /// </summary>
    [JsonIgnore]
    public bool HasFailures => Failed?.Count > 0;

    /// <summary>
    /// Gets a value indicating whether all messages were successfully sent.
    /// </summary>
    [JsonIgnore]
    public bool AllSuccessful => Failed is null || Failed.Count == 0;
}
