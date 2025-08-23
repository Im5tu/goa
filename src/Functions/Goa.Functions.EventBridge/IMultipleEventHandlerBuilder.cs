using Goa.Functions.Core;

namespace Goa.Functions.EventBridge;

/// <summary>
/// Builder interface for configuring batch event handlers
/// </summary>
public interface IMultipleEventHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process batches of EventBridge events
    /// Note: EventBridge typically sends single events, but this provides consistency with other event sources
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a collection of EventBridge events</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<EventbridgeEvent>, Task> handler) where THandler : class;
}