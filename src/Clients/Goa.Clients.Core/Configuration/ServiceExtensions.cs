using Goa.Clients.Core.Credentials;
using Goa.Clients.Core.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace Goa.Clients.Core.Configuration;

/// <summary>
/// Extension methods for configuring Goa services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds static AWS credentials to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add credentials to.</param>
    /// <param name="accessKeyId">The AWS access key ID.</param>
    /// <param name="secretAccessKey">The AWS secret access key.</param>
    /// <param name="sessionToken">Optional session token for temporary credentials.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when accessKeyId or secretAccessKey is null or whitespace.</exception>
    public static IServiceCollection AddStaticCredentials(this IServiceCollection services, string accessKeyId, string secretAccessKey, string? sessionToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretAccessKey);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICredentialProvider>(new StaticCredentialProvider(accessKeyId, secretAccessKey, sessionToken)));
        return services;
    }

    internal static IServiceCollection AddGoaService(this IServiceCollection services, string serviceName)
    {
        services.AddHttpClient(serviceName)
            .RemoveAllLoggers()
            .AddHttpMessageHandler<RequestSigningHandler>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestVersion = HttpVersion.Version30;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                client.Timeout = TimeSpan.FromSeconds(10);
            });

        services.TryAddTransient<RequestSigningHandler>();

        // Credential provider chain order matches AWS SDK:
        // 1. Environment Variables (highest priority)
        // 2. Config/Credentials Files (profile-based)
        // 3. Container/Instance Profile (lowest priority)
        // Note: Order is reversed by CredentialProviderChain, so last added = highest priority
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICredentialProvider, InstanceProfileCredentialProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICredentialProvider, ConfigCredentialProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICredentialProvider, EnvironmentCredentialProvider>());

        services.TryAddSingleton<ICredentialProviderChain, CredentialProviderChain>();

        return services;
    }
}
