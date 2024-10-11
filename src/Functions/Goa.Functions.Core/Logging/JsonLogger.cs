using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Core;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core.Logging;

internal sealed class JsonLogger : ILogger
{
    private static readonly Type SerializedType = typeof(IDictionary<string, object>);

    private static readonly string FieldTimestamp = "time";
    private static readonly string FieldMessage = "message";
    private static readonly string FieldLevel = "level";
    private static readonly string FieldVersion = "frameworkVersion";
    private static readonly string FieldException = "exception";
    private static readonly string FieldCategory = "category";
    private static readonly string FieldState = "category";

    private static readonly string LogLevelNone = nameof(LogLevel.None);
    private static readonly string LogLevelTrace = nameof(LogLevel.Trace);
    private static readonly string LogLevelDebug = nameof(LogLevel.Debug);
    private static readonly string LogLevelInformation = nameof(LogLevel.Information);
    private static readonly string LogLevelWarning = nameof(LogLevel.Warning);
    private static readonly string LogLevelError = nameof(LogLevel.Error);
    private static readonly string LogLevelCritical = nameof(LogLevel.Critical);

    private readonly string _categoryName;
    private readonly LogLevel _logLevel;
    private readonly IExternalScopeProvider _scopeProvider;
    private readonly JsonSerializerContext _jsonSerializerContext;

    public JsonLogger(string categoryName, LogLevel logLevel, IExternalScopeProvider scopeProvider, JsonSerializerContext jsonSerializerContext)
    {
        _categoryName = categoryName;
        _logLevel = logLevel;
        _scopeProvider = scopeProvider;
        _jsonSerializerContext = jsonSerializerContext;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            [FieldTimestamp] = DateTimeOffset.UtcNow.ToString("O"),
            [FieldMessage] = formatter(state, exception),
            [FieldLevel] = GetLogLevel(logLevel),
            [FieldVersion] = RuntimeInformation.FrameworkDescription,
            [FieldCategory] = _categoryName,
        };

        if (exception is not null)
        {
            data[FieldException] = exception.ToString();
        }

        if (state is IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            foreach (var (key, value) in parameters)
            {
                if ("{OriginalFormat}".EqualsIgnoreCase(key))
                {
                    continue;
                }

                data[key] = value;
            }
        }

        WriteScopeInformation(data, _scopeProvider);

        Console.WriteLine(JsonSerializer.Serialize(data, SerializedType, _jsonSerializerContext));
    }

    public bool IsEnabled(LogLevel logLevel) => _logLevel >= logLevel;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _scopeProvider.Push(state);
    }

    private static string GetLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogLevelTrace,
            LogLevel.Debug => LogLevelDebug,
            LogLevel.Information => LogLevelInformation,
            LogLevel.Warning => LogLevelWarning,
            LogLevel.Error => LogLevelError,
            LogLevel.Critical => LogLevelCritical,
            _ => LogLevelNone
        };
    }

    private static void WriteScopeInformation(Dictionary<string, object> data, IExternalScopeProvider scopeProvider)
    {
        scopeProvider.ForEachScope((scope, state) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> scopeItems)
            {
                foreach (var (key, value) in scopeItems)
                {
                    state[key] = value;
                }
            }
            else if (scope is KeyValuePair<string, string> kvp)
            {
                data[kvp.Key] = kvp.Value;
            }
            else if (scope is not null)
            {
                data[FieldState] = scope;
            }
        }, data);
    }
}
