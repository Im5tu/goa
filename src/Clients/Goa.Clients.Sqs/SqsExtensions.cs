using ErrorOr;
using Goa.Clients.Sqs.Operations.SendMessageBatch;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Goa.Clients.Sqs;

/// <summary>
/// Extension methods for ISqsClient to provide convenient batch operations with automatic chunking and parallel execution.
/// </summary>
public static class SqsExtensions
{
    /// <summary>
    /// Maximum number of entries per SQS batch request.
    /// </summary>
    private const int MaxBatchSize = 10;

    /// <summary>
    /// Sends multiple items as messages to an SQS queue, automatically chunking into batches of 10 and executing in parallel.
    /// </summary>
    /// <typeparam name="T">The type of items to serialize and send.</typeparam>
    /// <param name="client">The SQS client.</param>
    /// <param name="queueUrl">The URL of the queue.</param>
    /// <param name="items">The items to send.</param>
    /// <param name="jsonTypeInfo">The JSON type info for AOT serialization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated response from all batch operations.</returns>
    public static async Task<ErrorOr<SendMessageBatchAggregatedResponse>> SendMessageAsync<T>(
        this ISqsClient client,
        string queueUrl,
        IEnumerable<T> items,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        var entries = items.Select((item, index) => new SendMessageBatchRequestEntry
        {
            Id = index.ToString(),
            MessageBody = JsonSerializer.Serialize(item, jsonTypeInfo)
        }).ToList();

        return await SendBatchesInternalAsync(client, queueUrl, entries, cancellationToken);
    }

    /// <summary>
    /// Sends multiple items as messages to a FIFO SQS queue with message group IDs,
    /// automatically chunking into batches of 10 and executing in parallel.
    /// Deduplication IDs are auto-generated as GUIDs.
    /// </summary>
    /// <typeparam name="T">The type of items to serialize and send.</typeparam>
    /// <param name="client">The SQS client.</param>
    /// <param name="queueUrl">The URL of the FIFO queue.</param>
    /// <param name="items">Tuple of (item, messageGroupId) to send.</param>
    /// <param name="jsonTypeInfo">The JSON type info for AOT serialization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated response from all batch operations.</returns>
    public static async Task<ErrorOr<SendMessageBatchAggregatedResponse>> SendMessageAsync<T>(
        this ISqsClient client,
        string queueUrl,
        IEnumerable<(T Item, string MessageGroupId)> items,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        var entries = items.Select((tuple, index) => new SendMessageBatchRequestEntry
        {
            Id = index.ToString(),
            MessageBody = JsonSerializer.Serialize(tuple.Item, jsonTypeInfo),
            MessageGroupId = tuple.MessageGroupId,
            MessageDeduplicationId = Guid.NewGuid().ToString()
        }).ToList();

        return await SendBatchesInternalAsync(client, queueUrl, entries, cancellationToken);
    }

    /// <summary>
    /// Sends multiple items as messages to a FIFO SQS queue with message group IDs and custom deduplication IDs,
    /// automatically chunking into batches of 10 and executing in parallel.
    /// </summary>
    /// <typeparam name="T">The type of items to serialize and send.</typeparam>
    /// <param name="client">The SQS client.</param>
    /// <param name="queueUrl">The URL of the FIFO queue.</param>
    /// <param name="items">Tuple of (item, messageGroupId, messageDeduplicationId) to send.</param>
    /// <param name="jsonTypeInfo">The JSON type info for AOT serialization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated response from all batch operations.</returns>
    public static async Task<ErrorOr<SendMessageBatchAggregatedResponse>> SendMessageAsync<T>(
        this ISqsClient client,
        string queueUrl,
        IEnumerable<(T Item, string MessageGroupId, string MessageDeduplicationId)> items,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        var entries = items.Select((tuple, index) => new SendMessageBatchRequestEntry
        {
            Id = index.ToString(),
            MessageBody = JsonSerializer.Serialize(tuple.Item, jsonTypeInfo),
            MessageGroupId = tuple.MessageGroupId,
            MessageDeduplicationId = tuple.MessageDeduplicationId
        }).ToList();

        return await SendBatchesInternalAsync(client, queueUrl, entries, cancellationToken);
    }

    /// <summary>
    /// Internal method to chunk entries and send batches in parallel.
    /// </summary>
    private static async Task<ErrorOr<SendMessageBatchAggregatedResponse>> SendBatchesInternalAsync(
        ISqsClient client,
        string queueUrl,
        List<SendMessageBatchRequestEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return new SendMessageBatchAggregatedResponse { BatchCount = 0 };
        }

        // Chunk into batches of 10
        var chunks = entries.Chunk(MaxBatchSize).ToList();

        // Create batch requests
        var batchTasks = chunks.Select((chunk, batchIndex) =>
        {
            // Ensure unique IDs within each chunk (prefix with batch index)
            var adjustedEntries = chunk.Select((entry, entryIndex) => new SendMessageBatchRequestEntry
            {
                Id = $"{batchIndex}_{entryIndex}",
                MessageBody = entry.MessageBody,
                DelaySeconds = entry.DelaySeconds,
                MessageAttributes = entry.MessageAttributes,
                MessageSystemAttributes = entry.MessageSystemAttributes,
                MessageDeduplicationId = entry.MessageDeduplicationId,
                MessageGroupId = entry.MessageGroupId
            }).ToList();

            var request = new SendMessageBatchRequest
            {
                QueueUrl = queueUrl,
                Entries = adjustedEntries
            };

            return client.SendMessageBatchAsync(request, cancellationToken);
        }).ToList();

        // Execute all batches in parallel
        var results = await Task.WhenAll(batchTasks);

        // Aggregate results
        var aggregatedResponse = new SendMessageBatchAggregatedResponse
        {
            BatchCount = chunks.Count
        };

        foreach (var result in results)
        {
            if (result.IsError)
            {
                // If entire batch failed, return the errors
                return result.Errors;
            }

            if (result.Value.Successful != null)
            {
                aggregatedResponse.Successful.AddRange(result.Value.Successful);
            }

            if (result.Value.Failed != null)
            {
                aggregatedResponse.Failed.AddRange(result.Value.Failed);
            }
        }

        return aggregatedResponse;
    }
}
