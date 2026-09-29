using System.Globalization;
using ErrorOr;
using Goa.Clients.Sqs.Models;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibility;
using Goa.Clients.Sqs.Operations.DeleteMessage;

namespace Goa.Clients.Sqs.Consumer;

/// <summary>
/// A message received by <see cref="SqsConsumerExtensions.ConsumeAsync"/>, together with the operations that settle it.
/// Call <see cref="CompleteAsync"/> once the message has been processed; otherwise it is redelivered after its
/// visibility timeout expires and, once the queue's <c>maxReceiveCount</c> is reached, moved to the dead-letter queue.
/// </summary>
public sealed class SqsReceivedMessage
{
    private const string ApproximateReceiveCountAttribute = "ApproximateReceiveCount";
    private const string MessageGroupIdAttribute = "MessageGroupId";

    private readonly ISqsClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsReceivedMessage"/> class.
    /// </summary>
    /// <param name="client">The client used to settle the message.</param>
    /// <param name="queueUrl">The URL of the queue the message was received from.</param>
    /// <param name="message">The received message.</param>
    public SqsReceivedMessage(ISqsClient client, string queueUrl, SqsMessage message)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        ArgumentNullException.ThrowIfNull(message);

        _client = client;
        QueueUrl = queueUrl;
        Message = message;
    }

    /// <summary>
    /// The URL of the queue the message was received from.
    /// </summary>
    public string QueueUrl { get; }

    /// <summary>
    /// The received message, including its body, message attributes and system attributes.
    /// </summary>
    public SqsMessage Message { get; }

    /// <summary>
    /// The unique identifier of the message.
    /// </summary>
    public string? MessageId => Message.MessageId;

    /// <summary>
    /// The message body.
    /// </summary>
    public string? Body => Message.Body;

    /// <summary>
    /// The number of times the message has been received, or <c>null</c> unless
    /// <c>ApproximateReceiveCount</c> was requested via <see cref="SqsConsumerOptions.MessageSystemAttributeNames"/>.
    /// </summary>
    public int? ApproximateReceiveCount =>
        Message.Attributes is not null
        && Message.Attributes.TryGetValue(ApproximateReceiveCountAttribute, out var value)
        && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
            ? count
            : null;

    /// <summary>
    /// The FIFO message group of the message, or <c>null</c> for standard queues or unless
    /// <c>MessageGroupId</c> was requested via <see cref="SqsConsumerOptions.MessageSystemAttributeNames"/>.
    /// </summary>
    public string? MessageGroupId =>
        Message.Attributes is not null && Message.Attributes.TryGetValue(MessageGroupIdAttribute, out var value)
            ? value
            : null;

    /// <summary>
    /// Marks the message as processed by deleting it from the queue.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>Success, or the error returned by <c>DeleteMessage</c>.</returns>
    public async Task<ErrorOr<Success>> CompleteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _client.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = QueueUrl,
            ReceiptHandle = Message.ReceiptHandle ?? string.Empty
        }, cancellationToken);

        return result.IsError ? result.Errors : Result.Success;
    }

    /// <summary>
    /// Leaves the message on the queue for redelivery once its current visibility timeout expires.
    /// No request is sent, so the remaining visibility timeout acts as the retry delay.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>Success.</returns>
    public Task<ErrorOr<Success>> AbandonAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    /// <summary>
    /// Leaves the message on the queue and makes it visible again after <paramref name="redeliveryDelay"/>.
    /// Use <see cref="TimeSpan.Zero"/> for immediate redelivery.
    /// </summary>
    /// <param name="redeliveryDelay">How long until the message becomes visible again (0 seconds to 12 hours, rounded up to whole seconds).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>Success, or the error returned by <c>ChangeMessageVisibility</c>.</returns>
    public Task<ErrorOr<Success>> AbandonAsync(TimeSpan redeliveryDelay, CancellationToken cancellationToken = default)
    {
        return ChangeVisibilityAsync(redeliveryDelay, cancellationToken);
    }

    /// <summary>
    /// Keeps the message hidden from other consumers for <paramref name="visibilityTimeout"/>, measured from now.
    /// Use this to extend processing of long-running messages before the current visibility timeout expires.
    /// </summary>
    /// <param name="visibilityTimeout">The new visibility timeout (0 seconds to 12 hours, rounded up to whole seconds).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>Success, or the error returned by <c>ChangeMessageVisibility</c>.</returns>
    public Task<ErrorOr<Success>> ExtendVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken = default)
    {
        return ChangeVisibilityAsync(visibilityTimeout, cancellationToken);
    }

    private async Task<ErrorOr<Success>> ChangeVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        if (!SqsDurations.TryToSeconds(visibilityTimeout, SqsDurations.MaxVisibilityTimeout, out var seconds))
            return Error.Validation("SqsReceivedMessage.VisibilityTimeout", "Visibility timeout must be between 0 seconds and 12 hours.");

        var result = await _client.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = QueueUrl,
            ReceiptHandle = Message.ReceiptHandle ?? string.Empty,
            VisibilityTimeout = seconds
        }, cancellationToken);

        return result.IsError ? result.Errors : Result.Success;
    }
}
