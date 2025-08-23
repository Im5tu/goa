using Goa.Functions.Core;

namespace Goa.Functions.Sqs;

/// <summary>
/// Builder interface for configuring single message handlers
/// </summary>
public interface ISingleMessageHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process individual SQS messages
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single SQS message</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, SqsMessage, Task> handler) where THandler : class;
}