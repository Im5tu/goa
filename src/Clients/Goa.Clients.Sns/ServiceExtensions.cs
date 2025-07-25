using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sns;

/// <summary>
/// Extension methods for configuring SNS services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds SNS client services to the specified service collection with custom endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="serviceUrl">The custom SNS service endpoint URL.</param>
    /// <param name="region">The AWS region to use for SNS operations. Defaults to us-east-1.</param>
    /// <param name="logLevel">The minimum log level for SNS operations. Defaults to Information.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSns(this IServiceCollection services, string? serviceUrl = null, string? region = null, LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSns(config =>
        {
            config.ServiceUrl = serviceUrl;
            config.Region = region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            config.LogLevel = logLevel;
        });
    }

    /// <summary>
    /// Adds SNS client services to the specified service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the SNS service options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSns(this IServiceCollection services, Action<SnsServiceClientConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var configuration = new SnsServiceClientConfiguration();
        configureOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.Region) && string.IsNullOrWhiteSpace(configuration.ServiceUrl))
            throw new Exception("Either region or service url must be provided");

        return services.AddSnsCore(configuration);
    }

    private static IServiceCollection AddSnsCore(this IServiceCollection services, SnsServiceClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add the Goa service infrastructure for SNS
        services.AddGoaService(nameof(SnsServiceClient));

        // Register the configuration
        services.TryAddSingleton(configuration);

        // Register the SNS client
        services.TryAddTransient<ISnsClient, SnsServiceClient>();

        return services;
    }
}