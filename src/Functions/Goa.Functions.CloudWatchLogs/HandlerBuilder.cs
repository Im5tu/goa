using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.CloudWatchLogs;

internal sealed class HandlerBuilder : ISingleLogEventHandlerBuilder, IMultipleLogEventHandlerBuilder
{
    private readonly ILambdaBuilder _builder;
    private readonly bool _skipControlMessages;

    public HandlerBuilder(ILambdaBuilder builder, bool skipControlMessages)
    {
        _builder = builder;
        _skipControlMessages = skipControlMessages;
    }

    public IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogEvent, CloudWatchLogsEvent, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)CloudWatchLogsEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<CloudWatchLogsRawEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CloudWatchLogsEventHandler");

                return async (rawEvent, _, _) =>
                {
                    var logsEvent = DecompressEvent(rawEvent, logger);
                    if (logsEvent == null || (_skipControlMessages && logsEvent.IsControlMessage))
                    {
                        return new { };
                    }

                    foreach (var logEvent in logsEvent.LogEvents ?? [])
                    {
                        try
                        {
                            await handler(invocationHandler, logEvent, logsEvent);
                        }
                        catch (Exception e)
                        {
                            logger.LogException(e);
                            logEvent.MarkAsFailed();
                            throw;
                        }
                    }

                    return new { };
                };
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<CloudWatchLogsRawEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogsEvent, Task> handler) where THandler : class
    {
        var context = (JsonSerializerContext)CloudWatchLogsEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<CloudWatchLogsRawEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CloudWatchLogsEventHandler");

                return async (rawEvent, _, _) =>
                {
                    var logsEvent = DecompressEvent(rawEvent, logger);
                    if (logsEvent == null)
                    {
                        return new { };
                    }

                    try
                    {
                        await handler(invocationHandler, logsEvent);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        throw;
                    }

                    return new { };
                };
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<CloudWatchLogsRawEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }

    private static CloudWatchLogsEvent? DecompressEvent(CloudWatchLogsRawEvent rawEvent, ILogger logger)
    {
        if (string.IsNullOrEmpty(rawEvent.AwsLogs?.Data))
        {
            logger.LogWarning("Received CloudWatch Logs event with empty data");
            return null;
        }

        try
        {
            return CloudWatchLogsDecompressor.Decompress(
                rawEvent.AwsLogs.Data,
                CloudWatchLogsEventSerializationContext.Default.CloudWatchLogsEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decompress CloudWatch Logs event data");
            throw;
        }
    }
}
