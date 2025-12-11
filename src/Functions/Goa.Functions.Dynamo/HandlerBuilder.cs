using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Handler builder for DynamoDB stream Lambda functions
/// </summary>
internal sealed class HandlerBuilder : TypedHandlerBuilder<DynamoDbEvent, BatchItemFailureResponse>,
    ISingleRecordHandlerBuilder, IMultipleRecordHandlerBuilder
{
    public HandlerBuilder(ILambdaBuilder builder)
        : base(builder, DynamoDbEventSerializationContext.Default)
    {
    }

    /// <inheritdoc />
    protected override string GetLoggerName() => "DynamoDbEventHandler";

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, DynamoDbStreamRecord, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, ddbEvent, logger, ct) =>
        {
            var response = new BatchItemFailureResponse();
            var failedSequenceNumbers = new HashSet<string>();

            foreach (var record in ddbEvent.Records ?? [])
            {
                try
                {
                    await handler(h, record, ct);
                }
                catch (Exception e)
                {
                    logger.LogException(e);
                    record.MarkAsFailed();
                    var sequenceNumber = record.Dynamodb?.SequenceNumber;
                    if (sequenceNumber is not null)
                    {
                        failedSequenceNumbers.Add(sequenceNumber);
                        response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = sequenceNumber });
                    }
                }
            }

            // Check for records marked as failed without throwing an exception
            foreach (var record in ddbEvent.Records ?? [])
            {
                var sequenceNumber = record.Dynamodb?.SequenceNumber;
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
    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<DynamoDbStreamRecord>, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, ddbEvent, logger, ct) =>
        {
            var response = new BatchItemFailureResponse();

            try
            {
                await handler(h, ddbEvent.Records ?? [], ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                foreach (var record in ddbEvent.Records ?? [])
                {
                    record.MarkAsFailed();
                }
            }

            // Collect all failed records (from exception or MarkAsFailed() calls)
            foreach (var record in ddbEvent.Records ?? [])
            {
                var sequenceNumber = record.Dynamodb?.SequenceNumber;
                if (record.ProcessingType == ProcessingType.Failure && sequenceNumber is not null)
                {
                    response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = sequenceNumber });
                }
            }

            return response;
        });
    }
}
