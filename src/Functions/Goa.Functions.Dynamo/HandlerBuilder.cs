using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Dynamo;

internal sealed class HandlerBuilder : ISingleRecordHandlerBuilder, IMultipleRecordHandlerBuilder
{
    private readonly ILambdaBuilder _builder;

    public HandlerBuilder(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    public IRunnable HandleWith<THandler>(Func<THandler, DynamoDbStreamRecord, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)DynamoDbEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<DynamoDbEvent, InvocationRequest, CancellationToken, Task<string>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger =  sp.GetRequiredService<ILoggerFactory>().CreateLogger("DynamoDbEventHandler");
                Func<DynamoDbEvent, InvocationRequest, CancellationToken, Task<string>> result = async (ddbEvent, _, _) =>
                {
                    foreach (var record in ddbEvent.Records ?? Enumerable.Empty<DynamoDbStreamRecord>())
                    {
                        try
                        {
                            await handler(invocationHandler, record);
                        }
                        catch (Exception e)
                        {
                            logger.LogException(e);
                            record.MarkAsFailed();
                        }
                    }

                    return "";
                };
                return result;
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<DynamoDbEvent, string>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<DynamoDbStreamRecord>, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)DynamoDbEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<DynamoDbEvent, InvocationRequest, CancellationToken, Task<string>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger =  sp.GetRequiredService<ILoggerFactory>().CreateLogger("DynamoDbEventHandler");
                Func<DynamoDbEvent, InvocationRequest, CancellationToken, Task<string>> result = async (ddbEvent, _, _) =>
                {
                    try
                    {
                        await handler(invocationHandler, ddbEvent.Records ?? Enumerable.Empty<DynamoDbStreamRecord>());
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        foreach (var record in ddbEvent.Records ?? Enumerable.Empty<DynamoDbStreamRecord>())
                        {
                            record.MarkAsFailed();
                        }
                    }

                    return "";
                };
                return result;
            });

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<DynamoDbEvent, string>>(sp, context));
        });

        return new Runnable(_builder);
    }
}