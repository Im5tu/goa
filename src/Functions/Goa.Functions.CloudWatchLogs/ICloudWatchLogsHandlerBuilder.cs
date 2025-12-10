using Goa.Functions.Core;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Builder interface for configuring CloudWatch Logs event handlers.
/// Note: CloudWatch Logs uses a special decompression pattern, so this interface does not inherit from ITypedHandlerBuilder.
/// </summary>
public interface ICloudWatchLogsHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process CloudWatch Logs events
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes the CloudWatch Logs event</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogsEvent, Task> handler)
        where THandler : class
        => HandleWith<THandler>((h, evt, _) => handler(h, evt));

    /// <summary>
    /// Specifies the handler function to process CloudWatch Logs events with cancellation support
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes the CloudWatch Logs event with cancellation token</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogsEvent, CancellationToken, Task> handler)
        where THandler : class;
}
