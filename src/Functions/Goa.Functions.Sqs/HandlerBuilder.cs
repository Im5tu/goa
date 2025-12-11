using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.Sqs;

/// <summary>
/// Handler builder for SQS Lambda functions
/// </summary>
internal sealed class HandlerBuilder : TypedHandlerBuilder<SqsEvent, BatchItemFailureResponse>,
    ISingleMessageHandlerBuilder, IMultipleMessageHandlerBuilder
{
    public HandlerBuilder(ILambdaBuilder builder)
        : base(builder, SqsEventSerializationContext.Default)
    {
    }

    /// <inheritdoc />
    protected override string GetLoggerName() => "SqsEventHandler";

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, SqsMessage, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, sqsEvent, logger, ct) =>
        {
            var response = new BatchItemFailureResponse();
            var failedMessageIds = new HashSet<string>();

            foreach (var message in sqsEvent.Records ?? [])
            {
                try
                {
                    await handler(h, message, ct);
                }
                catch (Exception e)
                {
                    logger.LogException(e);
                    message.MarkAsFailed();
                    if (message.MessageId is not null)
                    {
                        failedMessageIds.Add(message.MessageId);
                        response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = message.MessageId });
                    }
                }
            }

            // Check for messages marked as failed without throwing an exception
            foreach (var message in sqsEvent.Records ?? [])
            {
                if (message.ProcessingType == ProcessingType.Failure &&
                    message.MessageId is not null &&
                    !failedMessageIds.Contains(message.MessageId))
                {
                    response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = message.MessageId });
                }
            }

            return response;
        });
    }

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<SqsMessage>, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, sqsEvent, logger, ct) =>
        {
            var response = new BatchItemFailureResponse();

            try
            {
                await handler(h, sqsEvent.Records ?? [], ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                foreach (var message in sqsEvent.Records ?? [])
                {
                    message.MarkAsFailed();
                }
            }

            // Collect all failed messages (from exception or MarkAsFailed() calls)
            foreach (var message in sqsEvent.Records ?? [])
            {
                if (message.ProcessingType == ProcessingType.Failure && message.MessageId is not null)
                {
                    response.BatchItemFailures.Add(new BatchItemFailure { ItemIdentifier = message.MessageId });
                }
            }

            return response;
        });
    }
}
