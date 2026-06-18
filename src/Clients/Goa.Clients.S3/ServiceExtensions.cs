using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.S3;

/// <summary>
/// Extension methods for configuring S3 services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds S3 client services to the specified service collection with custom endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="serviceUrl">The custom S3 service endpoint URL.</param>
    /// <param name="region">The AWS region to use for S3 operations. Defaults to us-east-1.</param>
    /// <param name="logLevel">The minimum log level for S3 operations. Defaults to Information.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddS3(this IServiceCollection services, string? serviceUrl = null, string? region = null, LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddS3(config =>
        {
            config.ServiceUrl = serviceUrl;
            config.Region = region ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            config.LogLevel = logLevel;
        });
    }

    /// <summary>
    /// Adds S3 client services to the specified service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the S3 service options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddS3(this IServiceCollection services, Action<S3ServiceClientConfiguration> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var configuration = new S3ServiceClientConfiguration();
        configureOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.Region) && string.IsNullOrWhiteSpace(configuration.ServiceUrl))
            throw new Exception("Either region or service url must be provided");

        // SigV4 signing always needs a region, even when only a ServiceUrl is configured. Apply the
        // same fallback as the other AddS3 overload so requests sign correctly when AWS_REGION is unset.
        if (string.IsNullOrWhiteSpace(configuration.Region))
            configuration.Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

        return services.AddS3Core(configuration);
    }

    private static IServiceCollection AddS3Core(this IServiceCollection services, S3ServiceClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add the Goa service infrastructure for S3
        services.AddGoaService(nameof(S3ServiceClient));

        // Register the configuration
        services.TryAddSingleton(configuration);

        // Register the S3 client
        services.TryAddTransient<IS3Client, S3ServiceClient>();

        return services;
    }
}
