using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Handler builder for Kinesis Lambda functions
/// </summary>
internal sealed class HandlerBuilder : TypedHandlerBuilder<KinesisEvent, BatchItemFailureResponse>,
    ISingleRecordHandlerBuilder, IMultipleRecordHandlerBuilder
{
    public HandlerBuilder(ILambdaBuilder builder)
        : base(builder, KinesisEventSerializationContext.Default)
    {
    }

    /// <inheritdoc />
    protected override string GetLoggerName() => "KinesisEventHandler";

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, KinesisRecord, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, kinesisEvent, logger, ct) =>
        {
            var response = new BatchItemFailureResponse();

            foreach (var record in kinesisEvent.Records ?? [])
            {
                try
                {
                    await handler(h, record, ct);
                }
                catch (Exception e)
                {
                    logger.LogException(e);
                    record.MarkAsFailed();
                    response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = record.Kinesis?.SequenceNumber });
                }
            }

            return response;
        });
    }

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<KinesisRecord>, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, kinesisEvent, logger, ct) =>
        {
            var response = new BatchItemFailureResponse();

            try
            {
                await handler(h, kinesisEvent.Records ?? [], ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                foreach (var record in kinesisEvent.Records ?? [])
                {
                    record.MarkAsFailed();
                    response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = record.Kinesis?.SequenceNumber });
                }
            }

            return response;
        });
    }
}
