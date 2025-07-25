using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.EventBridge;

/// <summary>
/// Extension methods for configuring EventBridge services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds EventBridge client services to the specified service collection with custom endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="serviceUrl">The custom EventBridge service endpoint URL.</param>
    /// <param name="region">The AWS region to use for EventBridge operations. Defaults to us-east-1.</param>
    /// <param name="logLevel">The minimum log level for EventBridge operations. Defaults to Information.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEventBridge(this IServiceCollection services, string? serviceUrl = null, string? region = null, LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddEventBridge(config =>
        {
            config.ServiceUrl = serviceUrl;
            config.Region = region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            config.LogLevel = logLevel;
        });
    }

    /// <summary>
    /// Adds EventBridge client services to the specified service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the EventBridge service options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEventBridge(this IServiceCollection services, Action<EventBridgeServiceClientConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var configuration = new EventBridgeServiceClientConfiguration();
        configureOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.Region) && string.IsNullOrWhiteSpace(configuration.ServiceUrl))
            throw new Exception("Either region or service url must be provided");

        return services.AddEventBridgeCore(configuration);
    }

    private static IServiceCollection AddEventBridgeCore(this IServiceCollection services, EventBridgeServiceClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add the Goa service infrastructure for EventBridge
        services.AddGoaService(nameof(EventBridgeServiceClient));

        // Register the configuration
        services.TryAddSingleton(configuration);

        // Register the EventBridge client
        services.TryAddTransient<IEventBridgeClient, EventBridgeServiceClient>();

        return services;
    }
}