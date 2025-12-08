using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.EventBridge;

/// <summary>
/// Handler builder for EventBridge Lambda functions
/// </summary>
internal sealed class HandlerBuilder : TypedHandlerBuilder<EventbridgeEvent, string>,
    ISingleEventHandlerBuilder, IMultipleEventHandlerBuilder
{
    public HandlerBuilder(ILambdaBuilder builder)
        : base(builder, EventbridgeEventSerializationContext.Default)
    {
    }

    /// <inheritdoc />
    protected override string GetLoggerName() => "EventbridgeEventHandler";

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, EventbridgeEvent, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, eventbridgeEvent, logger, ct) =>
        {
            try
            {
                await handler(h, eventbridgeEvent, ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                eventbridgeEvent.MarkAsFailed();
            }

            return "";
        });
    }

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<EventbridgeEvent>, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, eventbridgeEvent, logger, ct) =>
        {
            try
            {
                // EventBridge typically sends single events, so we create a collection with one item
                await handler(h, [eventbridgeEvent], ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                eventbridgeEvent.MarkAsFailed();
            }

            return "";
        });
    }
}
