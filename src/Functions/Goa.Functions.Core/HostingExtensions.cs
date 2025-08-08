using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Goa.Functions.Core;

/// <summary>
///     Extension methods for configuring Lambda hosting and lifecycle management.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    ///     Configures the host builder to use Lambda lifecycle management.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <param name="lambdaRuntimeClient">Optional override of the lambda runtime client</param>
    /// <returns>A configured <see cref="ILambdaBuilder"/> instance.</returns>
    public static ILambdaBuilder UseLambdaLifecycle(this IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        var lambdaBuilder = new LambdaBuilder(builder, lambdaRuntimeClient);
        if (lambdaRuntimeClient is not null)
            lambdaBuilder.WithServices(services => services.TryAddSingleton(lambdaRuntimeClient));

        return lambdaBuilder;
    }

    /// <summary>
    ///     Adds configuration to the Lambda builder.
    /// </summary>
    /// <param name="builder">The Lambda builder to configure.</param>
    /// <param name="configureDelegate">A delegate to configure the configuration builder.</param>
    /// <returns>The configured <see cref="ILambdaBuilder"/> instance.</returns>
    public static ILambdaBuilder WithConfiguration(this ILambdaBuilder builder, Action<IConfigurationBuilder> configureDelegate)
        => builder.WithConfiguration((_, configBuilder) => configureDelegate(configBuilder));

    /// <summary>
    ///     Adds services to the Lambda builder.
    /// </summary>
    /// <param name="builder">The Lambda builder to configure.</param>
    /// <param name="servicesDelegate">A delegate to configure the service collection.</param>
    /// <returns>The configured <see cref="ILambdaBuilder"/> instance.</returns>
    public static ILambdaBuilder WithServices(this ILambdaBuilder builder, Action<IServiceCollection> servicesDelegate)
        => builder.WithServices((_, services) => servicesDelegate(services));

    /// <summary>
    ///     Registers an initialization task that will be executed during Lambda startup.
    /// </summary>
    /// <typeparam name="T">The type of initialization task to register.</typeparam>
    /// <param name="builder">The Lambda builder to configure.</param>
    /// <returns>The configured <see cref="ILambdaBuilder"/> instance.</returns>
    public static ILambdaBuilder WithInitializationTask<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILambdaBuilder builder) where T : class, ILambdaInitializationTask
        => builder.WithServices(services => services.TryAddEnumerable(ServiceDescriptor.Singleton<ILambdaInitializationTask, T>()));
}
