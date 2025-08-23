using Goa.Functions.Core;

namespace Goa.Functions.Sqs;

/// <summary>
/// Builder interface for configuring batch message handlers
/// </summary>
public interface IMultipleMessageHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process batches of SQS messages
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a collection of SQS messages</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<SqsMessage>, Task> handler) where THandler : class;
}