using System.Runtime.CompilerServices;
using ErrorOr;
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
    /// Continuously long-polls <see cref="SqsConsumerOptions.QueueUrl"/> and yields each received message with a
    /// handle to complete (delete), abandon or extend it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sequence only ends when <paramref name="cancellationToken"/> is cancelled; cancellation ends the
    /// enumeration without throwing. Failed receives are logged and retried with exponential backoff and jitter
    /// (see <see cref="SqsConsumerOptions.InitialRetryDelay"/>), so transient errors never fault the sequence.
    /// </para>
    /// <para>
    /// Messages are never deleted automatically. A message that is not completed, including any message from a
    /// received batch that was not yet yielded when enumeration stopped, becomes visible again once its
    /// visibility timeout expires.
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
        return ConsumeCoreAsync(client, request, options.InitialRetryDelay, options.MaxRetryDelay, logger ?? NullLogger.Instance, cancellationToken);
    }

    private static async IAsyncEnumerable<SqsReceivedMessage> ConsumeCoreAsync(
        ISqsClient client,
        ReceiveMessageRequest request,
        TimeSpan initialRetryDelay,
        TimeSpan maxRetryDelay,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var consecutiveFailures = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await ReceiveAsync(client, request, logger, cancellationToken);

            // The client reports cancellation as an error rather than throwing, so check the token first.
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (result.IsError)
            {
                consecutiveFailures++;
                var delay = GetRetryDelay(consecutiveFailures, initialRetryDelay, maxRetryDelay);
                logger.ReceiveRetrying(request.QueueUrl, result.FirstError.Code, result.FirstError.Description, delay.TotalMilliseconds, consecutiveFailures);

                if (!await TryDelayAsync(delay, cancellationToken))
                    yield break;

                continue;
            }

            consecutiveFailures = 0;

            foreach (var message in result.Value.Messages)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return new SqsReceivedMessage(client, request.QueueUrl, message);
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

        if (options.MaxNumberOfMessages is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxNumberOfMessages, "MaxNumberOfMessages must be between 1 and 10.");

        if (!SqsDurations.TryToSeconds(options.WaitTime, SqsDurations.MaxWaitTime, out var waitTimeSeconds))
            throw new ArgumentOutOfRangeException(nameof(options), options.WaitTime, "WaitTime must be between 0 and 20 seconds.");

        int? visibilityTimeoutSeconds = null;
        if (options.VisibilityTimeout is { } visibilityTimeout)
        {
            if (!SqsDurations.TryToSeconds(visibilityTimeout, SqsDurations.MaxVisibilityTimeout, out var seconds))
                throw new ArgumentOutOfRangeException(nameof(options), visibilityTimeout, "VisibilityTimeout must be between 0 seconds and 12 hours.");
            visibilityTimeoutSeconds = seconds;
        }

        if (options.InitialRetryDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), options.InitialRetryDelay, "InitialRetryDelay must be greater than zero.");

        if (options.MaxRetryDelay < options.InitialRetryDelay)
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxRetryDelay, "MaxRetryDelay must be greater than or equal to InitialRetryDelay.");

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
