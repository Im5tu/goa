using Goa.Functions.Core;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Builder interface for configuring single log event handlers.
/// Note: CloudWatch Logs uses a special decompression pattern, so this interface does not inherit from ITypedHandlerBuilder.
/// </summary>
public interface ISingleLogEventHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process individual log events.
    /// The CloudWatchLogsEvent context provides access to log group, stream, and subscription filter info.
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single log event with event context</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogEvent, CloudWatchLogsEvent, Task> handler)
        where THandler : class
        => HandleWith<THandler>((h, evt, ctx, _) => handler(h, evt, ctx));

    /// <summary>
    /// Specifies the handler function to process individual log events with cancellation support.
    /// The CloudWatchLogsEvent context provides access to log group, stream, and subscription filter info.
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single log event with event context and cancellation token</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogEvent, CloudWatchLogsEvent, CancellationToken, Task> handler)
        where THandler : class;
}
