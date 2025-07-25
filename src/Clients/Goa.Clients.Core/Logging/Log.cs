using Microsoft.Extensions.Logging;

namespace Goa.Clients.Core.Logging;

internal static partial class Log
{
    [LoggerMessage(EventId = 1, Message = "Request started")]
    public static partial void RequestStart(this ILogger logger, LogLevel logLevel);
    [LoggerMessage(EventId = 2, Message = "Request completed in {duration:0.##}ms")]
    public static partial void RequestComplete(this ILogger logger, LogLevel logLevel, double duration);
    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Request failed. {message}")]
    public static partial void RequestFailed(this ILogger logger, string message);
    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Request failed")]
    public static partial void RequestFailed(this ILogger logger, Exception exception);
}