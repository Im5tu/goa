using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Logging;

/// <summary>
///     Helper to add Logging to a project
/// </summary>
public static class JsonLoggingProviderExtensions
{
    /// <summary>
    ///     Clears out the currently registered providers and adds our JsonLogger
    /// </summary>
    /// <returns></returns>
    public static ILoggingBuilder AddGoaJsonLogging(this ILoggingBuilder builder, JsonSerializerContext? jsonSerializerContext = null)
    {
        return builder.ClearProviders().AddProvider(new JsonLoggingProvider(LogLevel.Trace, jsonSerializerContext));
    }
}
