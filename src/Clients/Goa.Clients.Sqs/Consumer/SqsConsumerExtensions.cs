using System.Globalization;
using System.Runtime.CompilerServices;
using ErrorOr;
using Goa.Clients.Sqs.Models;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Goa.Clients.Sqs.Consumer;

/// <summary>
/// Extension methods for consuming an SQS queue as an <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class SqsConsumerExtensions
{
    /// <summary>
    /// The number of consecutive failed receives after which failures are logged as errors rather than warnings.
    /// </summary>
    internal const int ErrorLogThreshold = 5;

    private const int MaxBatchSize = 10;

    /// <summary>
    /// The time allowed for releasing unprocessed messages once enumeration has stopped.
    /// </summary>
    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Continuously long-polls <see cref="SqsConsumerOptions.QueueUrl"/> and yields each received message with a
    /// handle to complete (delete), abandon or extend it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sequence only ends when <paramref name="cancellationToken"/> is cancelled; cancellation ends the
    /// enumeration without throwing. Failed receives are logged and retried with exponential backoff and jitter
    /// (see <see cref="SqsConsumerOptions.InitialRetryDelay"/>), so errors never fault the sequence. From the fifth
    /// consecutive failure they are logged as errors instead of warnings, and every failure is reported to
    /// <see cref="SqsConsumerOptions.OnReceiveError"/>.
    /// </para>
    /// <para>
    /// Messages are never deleted automatically. A yielded message that is not completed becomes visible again once
    /// its visibility timeout expires. Messages that were received but not yet yielded when enumeration stopped
    /// (cancellation, or the caller leaving the loop) are released on a best-effort basis by resetting their
    /// visibility timeout to zero, so they can be redelivered straight away.
    /// </para>
    /// </remarks>
    /// <param name="client">The SQS client.</param>
    /// <param name="options">The consumer options.</param>
    /// <param name="logger">An optional logger used to report failed receives.</param>
    /// <param name="cancellationToken">A cancellation token that stops the consumer.</param>
    /// <returns>An async sequence of received messages.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options"/> contains an invalid value.</exception>
    public static IAsyncEnumerable<SqsReceivedMessage> ConsumeAsync(
        this ISqsClient client,
        SqsConsumerOptions options,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        var request = CreateReceiveRequest(options);
        return ConsumeCoreAsync(
            client,
            request,
            options.InitialRetryDelay,
            options.MaxRetryDelay,
            options.OnReceiveError,
            logger ?? NullLogger.Instance,
            cancellationToken);
    }

    private static async IAsyncEnumerable<SqsReceivedMessage> ConsumeCoreAsync(
        ISqsClient client,
        ReceiveMessageRequest request,
        TimeSpan initialRetryDelay,
        TimeSpan maxRetryDelay,
        Action<Error, int>? onReceiveError,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queueUrl = request.QueueUrl;
        var consecutiveFailures = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await ReceiveAsync(client, request, logger, cancellationToken);

            // The client reports cancellation as an error rather than throwing, so check the token first.
            if (cancellationToken.IsCancellationRequested)
            {
                if (!result.IsError)
                    await ReleaseAsync(client, queueUrl, result.Value?.Messages, 0, logger);
                yield break;
            }

            if (result.IsError)
            {
                consecutiveFailures++;
                var error = result.FirstError;
                var delay = GetRetryDelay(consecutiveFailures, initialRetryDelay, maxRetryDelay);

                if (consecutiveFailures >= ErrorLogThreshold)
                    logger.ReceiveStillFailing(queueUrl, error.Code, error.Description, delay.TotalMilliseconds, consecutiveFailures);
                else
                    logger.ReceiveRetrying(queueUrl, error.Code, error.Description, delay.TotalMilliseconds, consecutiveFailures);

                onReceiveError?.Invoke(error, consecutiveFailures);

                if (!await TryDelayAsync(delay, cancellationToken))
                    yield break;

                continue;
            }

            consecutiveFailures = 0;

            // Tolerate an empty response body or a null message list: both mean no messages.
            var messages = result.Value?.Messages;
            if (messages is null || messages.Count == 0)
                continue;

            var handedOver = 0;
            try
            {
                while (handedOver < messages.Count)
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;

                    var message = messages[handedOver++];
                    if (message is null)
                        continue;

                    yield return new SqsReceivedMessage(client, queueUrl, message);
                }
            }
            finally
            {
                // Runs on cancellation and when the caller stops enumerating mid-batch.
                if (handedOver < messages.Count)
                    await ReleaseAsync(client, queueUrl, messages, handedOver, logger);
            }
        }
    }

    private static async Task<ErrorOr<ReceiveMessageResponse>> ReceiveAsync(ISqsClient client, ReceiveMessageRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            return await client.ReceiveMessageAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Error.Failure("SQS.Consumer.Cancelled", "The consumer was cancelled.");
        }
        catch (Exception ex)
        {
            // ISqsClient reports failures as errors; treat an unexpected throw as a failed receive so the consumer keeps running.
            logger.ReceiveThrew(ex, request.QueueUrl);
            return Error.Unexpected("SQS.Consumer.ReceiveThrew", ex.Message);
        }
    }

    /// <summary>
    /// Best effort: makes received messages that were never handed to the caller visible again immediately.
    /// Never throws; on failure the messages reappear when their visibility timeout expires.
    /// </summary>
    private static async Task ReleaseAsync(ISqsClient client, string queueUrl, List<SqsMessage>? messages, int start, ILogger logger)
    {
        if (messages is null || start >= messages.Count)
            return;

        var entries = new List<ChangeMessageVisibilityBatchRequestEntry>(messages.Count - start);
        for (var i = start; i < messages.Count; i++)
        {
            var receiptHandle = messages[i]?.ReceiptHandle;
            if (!string.IsNullOrEmpty(receiptHandle))
            {
                entries.Add(new ChangeMessageVisibilityBatchRequestEntry
                {
                    Id = i.ToString(CultureInfo.InvariantCulture),
                    ReceiptHandle = receiptHandle,
                    VisibilityTimeout = 0
                });
            }
        }

        if (entries.Count == 0)
            return;

        // The consumer's token is usually cancelled by now, so use a short independent budget.
        using var timeoutCts = new CancellationTokenSource(ReleaseTimeout);
        try
        {
            foreach (var chunk in entries.Chunk(MaxBatchSize))
            {
                var result = await client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
                {
                    QueueUrl = queueUrl,
                    Entries = [.. chunk]
                }, timeoutCts.Token);

                if (result.IsError)
                    logger.ReleaseFailed(queueUrl, chunk.Length, result.FirstError.Code);
                else if (result.Value?.Failed is { Count: > 0 } failed)
                    logger.ReleaseFailed(queueUrl, failed.Count, failed[0].Code ?? "Unknown");
            }
        }
        catch (Exception ex)
        {
            logger.ReleaseThrew(ex, queueUrl);
        }
    }

    private static async Task<bool> TryDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    /// Exponential backoff with "equal jitter": half the capped exponential delay plus a random share of the other half.
    /// </summary>
    internal static TimeSpan GetRetryDelay(int consecutiveFailures, TimeSpan initialRetryDelay, TimeSpan maxRetryDelay)
    {
        var exponent = Math.Min(consecutiveFailures - 1, 30);
        var cappedMs = Math.Min(maxRetryDelay.TotalMilliseconds, initialRetryDelay.TotalMilliseconds * Math.Pow(2, exponent));
        var half = cappedMs / 2;
        return TimeSpan.FromMilliseconds(half + (Random.Shared.NextDouble() * half));
    }

    private static ReceiveMessageRequest CreateReceiveRequest(SqsConsumerOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.QueueUrl, $"{nameof(options)}.{nameof(SqsConsumerOptions.QueueUrl)}");

        if (options.MaxNumberOfMessages is < 1 or > MaxBatchSize)
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxNumberOfMessages, "MaxNumberOfMessages must be between 1 and 10.");

        if (options.WaitTime < SqsDurations.MinWaitTime || !SqsDurations.TryToSeconds(options.WaitTime, SqsDurations.MaxWaitTime, out var waitTimeSeconds))
            throw new ArgumentOutOfRangeException(nameof(options), options.WaitTime, "WaitTime must be between 1 and 20 seconds.");

        int? visibilityTimeoutSeconds = null;
        if (options.VisibilityTimeout is { } visibilityTimeout)
        {
            if (!SqsDurations.TryToSeconds(visibilityTimeout, SqsDurations.MaxVisibilityTimeout, out var seconds))
                throw new ArgumentOutOfRangeException(nameof(options), visibilityTimeout, "VisibilityTimeout must be between 0 seconds and 12 hours.");
            visibilityTimeoutSeconds = seconds;
        }

        if (options.InitialRetryDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), options.InitialRetryDelay, "InitialRetryDelay must be greater than zero.");

        if (options.MaxRetryDelay < options.InitialRetryDelay || options.MaxRetryDelay > SqsDurations.MaxRetryDelay)
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxRetryDelay, "MaxRetryDelay must be between InitialRetryDelay and 1 hour.");

        return new ReceiveMessageRequest
        {
            QueueUrl = options.QueueUrl,
            MaxNumberOfMessages = options.MaxNumberOfMessages,
            WaitTimeSeconds = waitTimeSeconds,
            VisibilityTimeout = visibilityTimeoutSeconds,
            MessageAttributeNames = options.MessageAttributeNames is null ? null : [.. options.MessageAttributeNames],
            MessageSystemAttributeNames = options.MessageSystemAttributeNames is null ? null : [.. options.MessageSystemAttributeNames]
        };
    }
}
