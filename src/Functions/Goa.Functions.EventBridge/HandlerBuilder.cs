using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.EventBridge;

internal sealed class HandlerBuilder : ISingleEventHandlerBuilder, IMultipleEventHandlerBuilder
{
    private readonly ILambdaBuilder _builder;

    public HandlerBuilder(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    public IRunnable HandleWith<THandler>(Func<THandler, EventbridgeEvent, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)EventbridgeEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<EventbridgeEvent, InvocationRequest, CancellationToken, Task<string>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EventbridgeEventHandler");
                Func<EventbridgeEvent, InvocationRequest, CancellationToken, Task<string>> result = async (eventbridgeEvent, _, _) =>
                {
                    try
                    {
                        await handler(invocationHandler, eventbridgeEvent);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        eventbridgeEvent.MarkAsFailed();
                    }

                    return "";
                };
                return result;
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<EventbridgeEvent, string>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<EventbridgeEvent>, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)EventbridgeEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<EventbridgeEvent, InvocationRequest, CancellationToken, Task<string>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EventbridgeEventHandler");
                Func<EventbridgeEvent, InvocationRequest, CancellationToken, Task<string>> result = async (eventbridgeEvent, _, _) =>
                {
                    try
                    {
                        // EventBridge typically sends single events, so we create a collection with one item
                        await handler(invocationHandler, new[] { eventbridgeEvent });
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        eventbridgeEvent.MarkAsFailed();
                    }

                    return "";
                };
                return result;
            });

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<EventbridgeEvent, string>>(sp, context));
        });

        return new Runnable(_builder);
    }
}