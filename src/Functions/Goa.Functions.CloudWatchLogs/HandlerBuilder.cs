using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Generic;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Handler builder for CloudWatch Logs Lambda functions
/// </summary>
internal sealed class HandlerBuilder : TypedHandlerBuilder<CloudWatchLogsRawEvent, string>,
    ICloudWatchLogsHandlerBuilder
{
    private readonly bool _skipControlMessages;

    public HandlerBuilder(ILambdaBuilder builder, bool skipControlMessages)
        : base(builder, CloudWatchLogsEventSerializationContext.Default)
    {
        _skipControlMessages = skipControlMessages;
    }

    /// <inheritdoc />
    protected override string GetLoggerName() => "CloudWatchLogsEventHandler";

    /// <inheritdoc />
    public IRunnable HandleWith<THandler>(Func<THandler, CloudWatchLogsEvent, CancellationToken, Task> handler)
        where THandler : class
    {
        var skipControlMessages = _skipControlMessages;

        return HandleWithLogger<THandler>(async (h, rawEvent, logger, ct) =>
        {
            var logsEvent = DecompressEvent(rawEvent, logger);
            if (logsEvent == null || (skipControlMessages && logsEvent.IsControlMessage))
            {
                return "";
            }

            try
            {
                await handler(h, logsEvent, ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                throw;
            }

            return "";
        });
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
