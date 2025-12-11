namespace Goa.Clients.Sqs.Operations.SendMessageBatch;

/// <summary>
/// Aggregated response from multiple batch send operations.
/// </summary>
public sealed class SendMessageBatchAggregatedResponse
{
    /// <summary>
    /// All successfully sent messages across all batches.
    /// </summary>
    public List<SendMessageBatchResultEntry> Successful { get; } = [];

    /// <summary>
    /// All failed messages across all batches.
    /// </summary>
    public List<BatchResultErrorEntry> Failed { get; } = [];

    /// <summary>
    /// Total number of batches executed.
    /// </summary>
    public int BatchCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are any failed entries.
    /// </summary>
    public bool HasFailures => Failed.Count > 0;

    /// <summary>
    /// Gets a value indicating whether all messages were successfully sent.
    /// </summary>
    public bool AllSuccessful => Failed.Count == 0;

    /// <summary>
    /// Total count of messages successfully sent.
    /// </summary>
    public int SuccessCount => Successful.Count;

    /// <summary>
    /// Total count of messages that failed.
    /// </summary>
    public int FailureCount => Failed.Count;
}
