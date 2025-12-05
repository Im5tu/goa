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
        // Check for LOGGING__LOGLEVEL__DEFAULT environment variable
        var logLevelEnv = Environment.GetEnvironmentVariable("LOGGING__LOGLEVEL__DEFAULT");
        var minimumLogLevel = LogLevel.Information; // Default value

        if (!Enum.TryParse<LogLevel>(logLevelEnv, ignoreCase: true, out var parsedLogLevel))
        {
            minimumLogLevel = parsedLogLevel;
        }

        builder.SetMinimumLevel(minimumLogLevel);
        builder.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.None);
        builder.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Information);

        return builder.ClearProviders().AddProvider(new JsonLoggingProvider(minimumLogLevel, jsonSerializerContext));
    }
}
