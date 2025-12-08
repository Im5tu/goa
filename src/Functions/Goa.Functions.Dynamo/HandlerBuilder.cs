using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Handler builder for DynamoDB stream Lambda functions
/// </summary>
internal sealed class HandlerBuilder : TypedHandlerBuilder<DynamoDbEvent, string>,
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
                }
            }

            return "";
        });
    }

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<DynamoDbStreamRecord>, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, ddbEvent, logger, ct) =>
        {
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

            return "";
        });
    }
}
