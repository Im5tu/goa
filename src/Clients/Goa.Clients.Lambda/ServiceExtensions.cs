using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Lambda;

/// <summary>
/// Extension methods for configuring Lambda services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds Lambda client services to the specified service collection with custom endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="serviceUrl">The custom Lambda service endpoint URL.</param>
    /// <param name="region">The AWS region to use for Lambda operations. Defaults to us-east-1.</param>
    /// <param name="logLevel">The minimum log level for Lambda operations. Defaults to Information.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLambda(this IServiceCollection services, string? serviceUrl = null, string? region = null, LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddLambda(config =>
        {
            config.ServiceUrl = serviceUrl;
            config.Region = region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            config.LogLevel = logLevel;
        });
    }

    /// <summary>
    /// Adds Lambda client services to the specified service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the Lambda service options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLambda(this IServiceCollection services, Action<LambdaServiceClientConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var configuration = new LambdaServiceClientConfiguration();
        configureOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.Region) && string.IsNullOrWhiteSpace(configuration.ServiceUrl))
            throw new Exception("Either region or service url must be provided");

        return services.AddLambdaCore(configuration);
    }

    private static IServiceCollection AddLambdaCore(this IServiceCollection services, LambdaServiceClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add the Goa service infrastructure for Lambda
        services.AddGoaService(nameof(LambdaServiceClient));

        // Register the configuration
        services.TryAddSingleton(configuration);

        // Register the Lambda client
        services.TryAddTransient<ILambdaClient, LambdaServiceClient>();

        return services;
    }
}