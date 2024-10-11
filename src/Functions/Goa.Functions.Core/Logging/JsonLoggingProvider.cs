using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Logging;

internal sealed class JsonLoggingProvider : ILoggerProvider
{
    private readonly JsonSerializerContext _jsonSerializerContext;
    private readonly LogLevel _logLevel;
    private readonly LogScopeProvider _scopeProvider;

    public JsonLoggingProvider(LogLevel level, JsonSerializerContext? jsonSerializerContext = null)
    {
        _logLevel = level;
        _scopeProvider = new LogScopeProvider();
        // TODO :: Document override
        _jsonSerializerContext = jsonSerializerContext ?? LoggingSerializationContext.Default;
    }

    public ILogger CreateLogger(string categoryName) => new JsonLogger(categoryName, _logLevel, _scopeProvider, _jsonSerializerContext);

    public void Dispose()
    {
    }
}
