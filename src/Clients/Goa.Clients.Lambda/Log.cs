using Microsoft.Extensions.Logging;

namespace Goa.Clients.Lambda;

internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to invoke Lambda function synchronously {FunctionName}")]
    public static partial void InvokeSynchronousFailed(this ILogger logger, Exception exception, string functionName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to invoke Lambda function asynchronously {FunctionName}")]
    public static partial void InvokeAsynchronousFailed(this ILogger logger, Exception exception, string functionName);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to dry run Lambda function {FunctionName}")]
    public static partial void InvokeDryRunFailed(this ILogger logger, Exception exception, string functionName);
}
