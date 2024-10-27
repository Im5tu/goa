using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Logging;

internal sealed class JsonLoggingProvider : ILoggerProvider
{
    private readonly JsonSerializerContext _jsonSerializerContext;
    private readonly LogLevel _logLevel;

    internal JsonLoggingProvider(LogLevel level, JsonSerializerContext? jsonSerializerContext = null)
    {
        _logLevel = level;
        _jsonSerializerContext = jsonSerializerContext ?? LoggingSerializationContext.Default;
    }

    public ILogger CreateLogger(string categoryName) => new JsonLogger(categoryName, _logLevel, LogScopeProvider.Instance, _jsonSerializerContext);

    public void Dispose()
    {
    }
}
