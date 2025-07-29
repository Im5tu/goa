using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core;

/// <summary>
///     Builder interface for configuring Lambda functions.
/// </summary>
public interface ILambdaBuilder
{
    /// <summary>
    ///     Access the host builder
    /// </summary>
    IHostBuilder Host { get; }

    /// <summary>
    ///     The lambda runtime that's currently in use
    /// </summary>
    ILambdaRuntimeClient LambdaRuntime { get; }

    /// <summary>
    ///     Configures the application configuration for the Lambda function.
    /// </summary>
    /// <param name="configureDelegate">A delegate to configure the configuration builder with host context.</param>
    /// <returns>The configured <see cref="ILambdaBuilder"/> instance.</returns>
    ILambdaBuilder WithConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate);
    /// <summary>
    ///     Configures the services for the Lambda function.
    /// </summary>
    /// <param name="servicesDelegate">A delegate to configure the service collection with host context.</param>
    /// <returns>The configured <see cref="ILambdaBuilder"/> instance.</returns>
    ILambdaBuilder WithServices(Action<HostBuilderContext, IServiceCollection> servicesDelegate);

    /// <summary>
    ///     Configures the logging serialization context. Needed for when you want to log something that's not on the builtin serializer
    /// </summary>
    /// <param name="jsonSerializerContext">The context to use</param>
    /// <returns>The configured <see cref="ILambdaBuilder"/> instance.</returns>
    ILambdaBuilder WithLoggingSerializationContext(JsonSerializerContext jsonSerializerContext);

    /// <summary>
    ///     Builds and runs the Lambda function host.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RunAsync(InitializationMode mode = InitializationMode.Parallel);
}
