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
            var failedSequenceNumbers = new HashSet<string>();

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
                    var sequenceNumber = record.Kinesis?.SequenceNumber;
                    if (sequenceNumber is not null)
                    {
                        failedSequenceNumbers.Add(sequenceNumber);
                        response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = sequenceNumber });
                    }
                }
            }

            // Check for records marked as failed without throwing an exception
            foreach (var record in kinesisEvent.Records ?? [])
            {
                var sequenceNumber = record.Kinesis?.SequenceNumber;
                if (record.ProcessingType == ProcessingType.Failure &&
                    sequenceNumber is not null &&
                    !failedSequenceNumbers.Contains(sequenceNumber))
                {
                    response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = sequenceNumber });
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
                }
            }

            // Collect all failed records (from exception or MarkAsFailed() calls)
            foreach (var record in kinesisEvent.Records ?? [])
            {
                var sequenceNumber = record.Kinesis?.SequenceNumber;
                if (record.ProcessingType == ProcessingType.Failure && sequenceNumber is not null)
                {
                    response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = sequenceNumber });
                }
            }

            return response;
        });
    }
}
