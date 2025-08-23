using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Kinesis;

internal sealed class HandlerBuilder : ISingleRecordHandlerBuilder, IMultipleRecordHandlerBuilder
{
    private readonly ILambdaBuilder _builder;

    public HandlerBuilder(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    public IRunnable Using<THandler>() where THandler : class
    {
        throw new NotImplementedException("Handler-based processing not yet implemented for Kinesis");
    }

    public IRunnable Using(Func<KinesisRecord, CancellationToken, Task> handler)
    {
        var context = (JsonSerializerContext)KinesisEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("KinesisEventHandler");
                Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>> result = async (kinesisEvent, _, cancellationToken) =>
                {
                    var failedRecords = new List<object>();

                    foreach (var record in kinesisEvent.Records ?? Enumerable.Empty<KinesisRecord>())
                    {
                        try
                        {
                            await handler(record, cancellationToken);
                        }
                        catch (Exception e)
                        {
                            logger.LogException(e);
                            record.MarkAsFailed();
                            failedRecords.Add(new { itemIdentifier = record.Kinesis?.SequenceNumber });
                        }
                    }

                    return new { batchItemFailures = failedRecords };
                };
                return result;
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<KinesisEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable Using<TResult>(Func<KinesisRecord, CancellationToken, Task<TResult>> handler)
    {
        var context = (JsonSerializerContext)KinesisEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("KinesisEventHandler");
                Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>> result = async (kinesisEvent, _, cancellationToken) =>
                {
                    var failedRecords = new List<object>();
                    var results = new List<TResult>();

                    foreach (var record in kinesisEvent.Records ?? Enumerable.Empty<KinesisRecord>())
                    {
                        try
                        {
                            var recordResult = await handler(record, cancellationToken);
                            results.Add(recordResult);
                        }
                        catch (Exception e)
                        {
                            logger.LogException(e);
                            record.MarkAsFailed();
                            failedRecords.Add(new { itemIdentifier = record.Kinesis?.SequenceNumber });
                        }
                    }

                    return new { batchItemFailures = failedRecords, results };
                };
                return result;
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<KinesisEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable Using(Func<KinesisEvent, CancellationToken, Task> handler)
    {
        var context = (JsonSerializerContext)KinesisEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("KinesisEventHandler");
                Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>> result = async (kinesisEvent, _, cancellationToken) =>
                {
                    var failedRecords = new List<object>();

                    try
                    {
                        await handler(kinesisEvent, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        foreach (var record in kinesisEvent.Records ?? Enumerable.Empty<KinesisRecord>())
                        {
                            record.MarkAsFailed();
                            failedRecords.Add(new { itemIdentifier = record.Kinesis?.SequenceNumber });
                        }
                    }

                    return new { batchItemFailures = failedRecords };
                };
                return result;
            });

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<KinesisEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable Using<TResult>(Func<KinesisEvent, CancellationToken, Task<TResult>> handler)
    {
        var context = (JsonSerializerContext)KinesisEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>>>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("KinesisEventHandler");
                Func<KinesisEvent, InvocationRequest, CancellationToken, Task<object>> result = async (kinesisEvent, _, cancellationToken) =>
                {
                    var failedRecords = new List<object>();

                    try
                    {
                        var batchResult = await handler(kinesisEvent, cancellationToken);
                        return new { batchItemFailures = failedRecords, result = batchResult };
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        foreach (var record in kinesisEvent.Records ?? Enumerable.Empty<KinesisRecord>())
                        {
                            record.MarkAsFailed();
                            failedRecords.Add(new { itemIdentifier = record.Kinesis?.SequenceNumber });
                        }
                        return new { batchItemFailures = failedRecords };
                    }
                };
                return result;
            });

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<KinesisEvent, object>>(sp, context));
        });

        return new Runnable(_builder);
    }
}