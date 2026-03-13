using Microsoft.Extensions.Logging;

namespace Goa.Clients.EventBridge;

internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to put events to EventBridge")]
    public static partial void PutEventsFailed(this ILogger logger, Exception exception);
}
