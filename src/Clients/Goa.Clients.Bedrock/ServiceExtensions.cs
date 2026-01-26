using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Bedrock;

/// <summary>
/// Extension methods for configuring Bedrock services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds Bedrock client services to the specified service collection with custom endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="serviceUrl">The custom Bedrock service endpoint URL.</param>
    /// <param name="region">The AWS region to use for Bedrock operations. Defaults to us-east-1.</param>
    /// <param name="logLevel">The minimum log level for Bedrock operations. Defaults to Information.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBedrock(this IServiceCollection services, string? serviceUrl = null, string? region = null, LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddBedrock(config =>
        {
            config.ServiceUrl = serviceUrl;
            config.Region = region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            config.LogLevel = logLevel;
        });
    }

    /// <summary>
    /// Adds Bedrock client services to the specified service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the Bedrock service options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBedrock(this IServiceCollection services, Action<BedrockServiceClientConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var configuration = new BedrockServiceClientConfiguration();
        configureOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.Region) && string.IsNullOrWhiteSpace(configuration.ServiceUrl))
            throw new Exception("Either region or service url must be provided");

        return services.AddBedrockCore(configuration);
    }

    private static IServiceCollection AddBedrockCore(this IServiceCollection services, BedrockServiceClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add the Goa service infrastructure for Bedrock
        services.AddGoaService(nameof(BedrockServiceClient));

        // Register the configuration
        services.TryAddSingleton(configuration);

        // Register the Bedrock client
        services.TryAddTransient<IBedrockClient, BedrockServiceClient>();

        return services;
    }
}
