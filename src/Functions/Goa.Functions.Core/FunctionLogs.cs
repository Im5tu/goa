using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core;

/// <summary>
///     Default function logging
/// </summary>
public static partial class FunctionLogs
{
    /// <summary>
    ///     Logs that a function completed in the specified amount of time
    /// </summary>
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Function completed in {Duration:0.##}ms")]
    public static partial void LogFunctionCompletion(this ILogger logger, double duration);

    /// <summary>
    ///     Logs that an error occurred whilst handling the request
    /// </summary>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Function failed to execute")]
    public static partial void LogFunctionError(this ILogger logger, Exception exception);

    /// <summary>
    ///     Logs that an error occurred whilst handling the request
    /// </summary>
    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "An error occured while executing the function: {message}")]
    public static partial void LogFunctionError(this ILogger logger, string message);
}
