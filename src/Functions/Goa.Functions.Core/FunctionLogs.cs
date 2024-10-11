using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core;

internal static partial class FunctionLogs
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Function completed in {Duration} ms")]
    public static partial void LogFunctionCompletion(this ILogger logger, double duration);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Function failed to execute")]
    public static partial void LogFunctionError(this ILogger logger, Exception exception);
}
