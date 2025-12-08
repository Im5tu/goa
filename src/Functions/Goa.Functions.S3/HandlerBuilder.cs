using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.S3;

/// <summary>
/// Handler builder for S3 Lambda functions
/// </summary>
internal sealed class HandlerBuilder : TypedHandlerBuilder<S3Event, string>,
    ISingleRecordHandlerBuilder, IMultipleRecordHandlerBuilder
{
    public HandlerBuilder(ILambdaBuilder builder)
        : base(builder, S3EventSerializationContext.Default)
    {
    }

    /// <inheritdoc />
    protected override string GetLoggerName() => "S3EventHandler";

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, S3EventRecord, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, s3Event, logger, ct) =>
        {
            foreach (var record in s3Event.Records ?? [])
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
    public IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<S3EventRecord>, CancellationToken, Task> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, s3Event, logger, ct) =>
        {
            try
            {
                await handler(h, s3Event.Records ?? [], ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                foreach (var record in s3Event.Records ?? [])
                {
                    record.MarkAsFailed();
                }
            }

            return "";
        });
    }
}
