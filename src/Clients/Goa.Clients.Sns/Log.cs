using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sns;

internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to publish message to SNS target {Target}")]
    public static partial void PublishFailed(this ILogger logger, Exception exception, string? target);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to deserialize SNS XML error response: {Content}")]
    public static partial void DeserializeSnsErrorFailed(this ILogger logger, Exception exception, string content);
}
