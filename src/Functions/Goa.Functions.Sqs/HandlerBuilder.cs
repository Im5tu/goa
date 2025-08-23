using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Sqs;

internal sealed class HandlerBuilder : ISingleMessageHandlerBuilder, IMultipleMessageHandlerBuilder
{
    private readonly ILambdaBuilder _builder;

    public HandlerBuilder(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    public IRunnable HandleWith<THandler>(Func<THandler, SqsMessage, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)SqsEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<SqsEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SqsEventHandler");
                Func<SqsEvent, InvocationRequest, CancellationToken, Task<object>> result = async (sqsEvent, _, _) =>
                {
                    var failedMessages = new List<object>();

                    foreach (var message in sqsEvent.Records ?? Enumerable.Empty<SqsMessage>())
                    {
                        try
                        {
                            await handler(invocationHandler, message);
                        }
                        catch (Exception e)
                        {
                            logger.LogException(e);
                            message.MarkAsFailed();
                            failedMessages.Add(new { itemIdentifier = message.MessageId });
                        }
                    }

                    return new { batchItemFailures = failedMessages };
                };
                return result;
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<SqsEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<SqsMessage>, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)SqsEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<SqsEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SqsEventHandler");
                Func<SqsEvent, InvocationRequest, CancellationToken, Task<object>> result = async (sqsEvent, _, _) =>
                {
                    var failedMessages = new List<object>();

                    try
                    {
                        await handler(invocationHandler, sqsEvent.Records ?? Enumerable.Empty<SqsMessage>());
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        foreach (var message in sqsEvent.Records ?? Enumerable.Empty<SqsMessage>())
                        {
                            message.MarkAsFailed();
                            failedMessages.Add(new { itemIdentifier = message.MessageId });
                        }
                    }

                    return new { batchItemFailures = failedMessages };
                };
                return result;
            });

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<SqsEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }
}