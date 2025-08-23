using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

internal sealed class HandlerBuilder : ISingleRecordHandlerBuilder, IMultipleRecordHandlerBuilder
{
    private readonly ILambdaBuilder _builder;

    public HandlerBuilder(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    public IRunnable HandleWith<THandler>(Func<THandler, S3EventRecord, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)S3EventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<S3Event, InvocationRequest, CancellationToken, Task<string>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("S3EventHandler");
                Func<S3Event, InvocationRequest, CancellationToken, Task<string>> result = async (s3Event, _, _) =>
                {
                    foreach (var record in s3Event.Records ?? Enumerable.Empty<S3EventRecord>())
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
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<S3Event, string>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<S3EventRecord>, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)S3EventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<S3Event, InvocationRequest, CancellationToken, Task<string>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("S3EventHandler");
                Func<S3Event, InvocationRequest, CancellationToken, Task<string>> result = async (s3Event, _, _) =>
                {
                    try
                    {
                        await handler(invocationHandler, s3Event.Records ?? Enumerable.Empty<S3EventRecord>());
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        foreach (var record in s3Event.Records ?? Enumerable.Empty<S3EventRecord>())
                        {
                            record.MarkAsFailed();
                        }
                    }

                    return "";
                };
                return result;
            });

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<S3Event, string>>(sp, context));
        });

        return new Runnable(_builder);
    }
}