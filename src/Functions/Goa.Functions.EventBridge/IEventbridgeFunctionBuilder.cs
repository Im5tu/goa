using Goa.Functions.Core;

namespace Goa.Functions.EventBridge;

/// <summary>
/// Builder interface for configuring EventBridge Lambda functions
/// </summary>
public interface IEventbridgeFunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the function to process EventBridge events one at a time
    /// </summary>
    /// <returns>A builder for configuring single event handlers</returns>
    ISingleEventHandlerBuilder ProcessOneAtATime();

    /// <summary>
    /// Configures the function to process EventBridge events as a batch
    /// Note: EventBridge typically sends single events, but this provides consistency with other event sources
    /// </summary>
    /// <returns>A builder for configuring batch event handlers</returns>
    IMultipleEventHandlerBuilder ProcessAsBatch();
}