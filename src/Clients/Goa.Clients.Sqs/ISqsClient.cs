using ErrorOr;
using Goa.Clients.Sqs.Operations.DeleteMessage;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Operations.SendMessage;
using Goa.Clients.Sqs.Operations.SendMessageBatch;

namespace Goa.Clients.Sqs;

/// <summary>
/// High-performance SQS client interface optimized for AWS Lambda usage.
/// All operations use strongly-typed request objects and return ErrorOr results.
/// </summary>
public interface ISqsClient
{
    /// <summary>
    /// Sends a message to the specified queue.
    /// </summary>
    /// <param name="request">The send message request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The send message response, or an error if the operation failed.</returns>
    Task<ErrorOr<SendMessageResponse>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends up to 10 messages to the specified queue in a single batch operation.
    /// </summary>
    /// <param name="request">The send message batch request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The send message batch response, or an error if the operation failed.</returns>
    Task<ErrorOr<SendMessageBatchResponse>> SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves one or more messages from the specified queue.
    /// </summary>
    /// <param name="request">The receive message request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The receive message response, or an error if the operation failed.</returns>
    Task<ErrorOr<ReceiveMessageResponse>> ReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified message from the specified queue.
    /// </summary>
    /// <param name="request">The delete message request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The delete message response, or an error if the operation failed.</returns>
    Task<ErrorOr<DeleteMessageResponse>> DeleteMessageAsync(DeleteMessageRequest request, CancellationToken cancellationToken = default);
}