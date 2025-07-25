using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Dynamo;

/// <summary>
/// Extension methods for configuring DynamoDB services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds DynamoDB client services to the specified service collection with custom endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="serviceUrl">The custom DynamoDB service endpoint URL.</param>
    /// <param name="region">The AWS region to use for DynamoDB operations. Defaults to us-east-1.</param>
    /// <param name="logLevel">The minimum log level for DynamoDB operations. Defaults to Information.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDynamoDB(this IServiceCollection services, string? serviceUrl = null, string? region = null, LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDynamoDB(config =>
        {
            config.ServiceUrl = serviceUrl;
            config.Region = region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            config.LogLevel = logLevel;
        });
    }

    /// <summary>
    /// Adds DynamoDB client services to the specified service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the DynamoDB service options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDynamoDB(this IServiceCollection services, Action<DynamoServiceClientConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var configuration = new DynamoServiceClientConfiguration();
        configureOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.Region) && string.IsNullOrWhiteSpace(configuration.ServiceUrl))
            throw new Exception("Either region or service url must be provided");

        return services.AddDynamoDBCore(configuration);
    }

    private static IServiceCollection AddDynamoDBCore(this IServiceCollection services, DynamoServiceClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add the Goa service infrastructure for DynamoDB
        services.AddGoaService(nameof(DynamoServiceClient));

        // Register the configuration
        services.TryAddSingleton(configuration);

        // Register the DynamoDB client
        services.TryAddTransient<IDynamoClient, DynamoServiceClient>();

        return services;
    }
}
