namespace Goa.Clients.Sqs.Consumer;

/// <summary>
/// Options for <see cref="SqsConsumerExtensions.ConsumeAsync"/>, a long-running long-polling SQS consumer.
/// </summary>
public sealed class SqsConsumerOptions
{
    /// <summary>
    /// The URL of the Amazon SQS queue to consume from.
    /// </summary>
    public required string QueueUrl { get; set; }

    /// <summary>
    /// The maximum number of messages to request per receive (1-10). Defaults to 10.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// How long each receive waits for messages to arrive (0-20 seconds, rounded up to whole seconds).
    /// Defaults to 20 seconds (long polling). A value of zero short-polls, which re-requests immediately when the queue is empty.
    /// </summary>
    public TimeSpan WaitTime { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The visibility timeout applied to received messages (0 seconds to 12 hours, rounded up to whole seconds).
    /// When <c>null</c> the queue's configured visibility timeout is used.
    /// </summary>
    public TimeSpan? VisibilityTimeout { get; set; }

    /// <summary>
    /// The message attributes to return with each message, e.g. <c>traceparent</c>, or <c>All</c>.
    /// </summary>
    public List<string>? MessageAttributeNames { get; set; }

    /// <summary>
    /// The system attributes to return with each message, e.g. <c>MessageGroupId</c>,
    /// <c>ApproximateReceiveCount</c>, <c>AWSTraceHeader</c>, or <c>All</c>.
    /// </summary>
    public List<string>? MessageSystemAttributeNames { get; set; }

    /// <summary>
    /// The delay before the first retry after a failed receive. Subsequent consecutive failures double the delay
    /// (with jitter) up to <see cref="MaxRetryDelay"/>. Defaults to 1 second.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The upper bound for the delay between retries of a failed receive. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
