using Goa.Functions.Core;

namespace Goa.Functions.Sqs;

/// <summary>
/// Builder interface for configuring single message handlers
/// </summary>
public interface ISingleMessageHandlerBuilder : ITypedHandlerBuilder<SqsEvent, BatchItemFailureResponse>
{
    /// <summary>
    /// Specifies the handler function to process individual SQS messages
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single SQS message</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, SqsMessage, Task> handler)
        where THandler : class
        => HandleWith<THandler>((h, msg, _) => handler(h, msg));

    /// <summary>
    /// Specifies the handler function to process individual SQS messages with cancellation support
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single SQS message with cancellation token</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, SqsMessage, CancellationToken, Task> handler)
        where THandler : class;
}
