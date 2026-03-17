using Microsoft.Extensions.Logging;

namespace Goa.Core;

/// <summary>
///     Extends all ILogger instances with commonly used extension methods
/// </summary>
public static partial class LoggerExtensions
{
    /// <summary>
    ///     Attaches a series of values to the context, using a Microsoft.Extensions.Logging compatible approach
    /// </summary>
    /// <param name="logger">The logger we are going to scope</param>
    /// <param name="values">The properties that we want to add to each log message in the scope</param>
    public static IDisposable? WithContext<T>(this ILogger logger, params KeyValuePair<string, T>[] values)
    {
        if (values.Length == 0)
            return null;

        var dict = new Dictionary<string, object>(values.Length);
        foreach (var kv in values)
        {
            // Boxing T→object is unavoidable: ILogger.BeginScope requires IDictionary<string, object>
#pragma warning disable GOA1501
            dict[kv.Key] = kv.Value!;
#pragma warning restore GOA1501
        }
        return logger.BeginScope(dict);
    }

    /// <summary>
    ///     Attaches a series of values to the context, using a Microsoft.Extensions.Logging compatible approach
    /// </summary>
    /// <param name="logger">The logger we are going to scope</param>
    /// <param name="values">The properties that we want to add to each log message in the scope</param>
    public static IDisposable? WithContext<T>(this ILogger logger, params (string key, T value)[] values)
    {
        if (values.Length == 0)
            return null;

        var dict = new Dictionary<string, object>(values.Length);
        foreach (var (key, value) in values)
        {
            // Boxing T→object is unavoidable: ILogger.BeginScope requires IDictionary<string, object>
#pragma warning disable GOA1501
            dict[key] = value!;
#pragma warning restore GOA1501
        }
        return logger.BeginScope(dict);
    }

    /// <summary>
    ///     Attaches a key/value pair to the context, using a Microsoft.Extensions.Logging compatible approach
    /// </summary>
    /// <param name="logger">The logger we are going to scope</param>
    /// <param name="key">The name of the property</param>
    /// <param name="value">The value of the property</param>
    public static IDisposable? WithContext<T>(this ILogger logger, string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        // Boxing T→object is unavoidable: ILogger.BeginScope requires IDictionary<string, object>
#pragma warning disable GOA1501
        return logger.BeginScope(new Dictionary<string, object>
        {
            [key] = value!
        });
#pragma warning restore GOA1501
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "{ExceptionMessage}")]
    private static partial void LogExceptionCritical(this ILogger logger, string? exceptionMessage);

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "{ExceptionMessage}")]
    private static partial void LogExceptionError(this ILogger logger, string? exceptionMessage);

    /// <summary>
    ///     Helper to log an exception when there is no specific message associated with it
    /// </summary>
    /// <param name="logger">The logger to write to</param>
    /// <param name="exception">The exception that was thrown</param>
    /// <param name="critical">Whether it was a critical exception that was thrown. If true, this gets logged as a critical level message</param>
    /// <typeparam name="T">The type of the exception thrown</typeparam>
    public static void LogException<T>(this ILogger logger, T exception, bool critical = false) where T : Exception
    {
#pragma warning disable GOA1401 // Explicit array required for params overload
        using var scope = logger.WithContext(new KeyValuePair<string, string?>[] { new("Exception:Type", exception.GetType().FullName), new("Exception:StackTrace", exception.StackTrace) });
#pragma warning restore GOA1401
        if (critical)
            logger.LogExceptionCritical(exception.Message);
        else
            logger.LogExceptionError(exception.Message);
    }
}
