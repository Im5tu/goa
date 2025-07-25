using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sqs;

/// <summary>
/// Extension methods for configuring SQS services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds SQS client services to the specified service collection with custom endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="serviceUrl">The custom SQS service endpoint URL.</param>
    /// <param name="region">The AWS region to use for SQS operations. Defaults to us-east-1.</param>
    /// <param name="logLevel">The minimum log level for SQS operations. Defaults to Information.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSqs(this IServiceCollection services, string? serviceUrl = null, string? region = null, LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSqs(config =>
        {
            config.ServiceUrl = serviceUrl;
            config.Region = region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            config.LogLevel = logLevel;
        });
    }

    /// <summary>
    /// Adds SQS client services to the specified service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the SQS service options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSqs(this IServiceCollection services, Action<SqsServiceClientConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var configuration = new SqsServiceClientConfiguration();
        configureOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.Region) && string.IsNullOrWhiteSpace(configuration.ServiceUrl))
            throw new Exception("Either region or service url must be provided");

        return services.AddSqsCore(configuration);
    }

    private static IServiceCollection AddSqsCore(this IServiceCollection services, SqsServiceClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add the Goa service infrastructure for SQS
        services.AddGoaService(nameof(SqsServiceClient));

        // Register the configuration
        services.TryAddSingleton(configuration);

        // Register the SQS client
        services.TryAddTransient<ISqsClient, SqsServiceClient>();

        return services;
    }
}