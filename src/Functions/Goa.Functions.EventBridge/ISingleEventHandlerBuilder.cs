using Goa.Functions.Core;

namespace Goa.Functions.EventBridge;

/// <summary>
/// Builder interface for configuring single event handlers
/// </summary>
public interface ISingleEventHandlerBuilder : ITypedHandlerBuilder<EventbridgeEvent, string>
{
    /// <summary>
    /// Specifies the handler function to process individual EventBridge events
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single EventBridge event</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, EventbridgeEvent, Task> handler)
        where THandler : class
        => HandleWith<THandler>((h, evt, _) => handler(h, evt));

    /// <summary>
    /// Specifies the handler function to process individual EventBridge events with cancellation support
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single EventBridge event with cancellation token</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, EventbridgeEvent, CancellationToken, Task> handler)
        where THandler : class;
}
