using Microsoft.Extensions.Logging;

namespace Goa.Core;

/// <summary>
///     Extends all ILogger instances with commonly used extension methods
/// </summary>
public static class LoggerExtensions
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

        return logger.BeginScope(values.ToDictionary(x => x.Key, x => (object)x.Value!));
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

        return logger.BeginScope(values.ToDictionary(x => x.key, x => (object)x.value!));
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

        return logger.BeginScope(new Dictionary<string, object>
        {
            [key] = value!
        });
    }

    /// <summary>
    ///     Helper to log an exception when there is no specific message associated with it
    /// </summary>
    /// <param name="logger">The logger to write to</param>
    /// <param name="exception">The exception that was thrown</param>
    /// <param name="critical">Whether it was a critical exception that was thrown. If true, this gets logged as a critical level message</param>
    /// <typeparam name="T">The type of the exception thrown</typeparam>
    public static void LogException<T>(this ILogger logger, T exception, bool critical = false) where T : Exception
    {
        // ReSharper disable TemplateIsNotCompileTimeConstantProblem
        if (critical)
            logger.LogCritical(exception, exception.Message);
        else
            logger.LogError(exception, exception.Message);
        // ReSharper restore TemplateIsNotCompileTimeConstantProblem
    }
}
