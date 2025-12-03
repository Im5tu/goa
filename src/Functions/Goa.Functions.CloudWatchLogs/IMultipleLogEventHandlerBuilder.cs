using Goa.Functions.Core;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Builder interface for configuring batch log event handlers
/// </summary>
public interface IMultipleLogEventHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process batches of log events
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes the entire CloudWatch Logs event</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogsEvent, Task> handler) where THandler : class;
}
