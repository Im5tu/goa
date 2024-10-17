using System.Diagnostics;
using System.Text.Json.Serialization;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Goa.Functions.Core;

/// <summary>
///     Represents the base class for all Goa function implementations, providing Dependency Injection (DI), logging, and configuration.
/// </summary>
/// <typeparam name="TRequest">The type of the request object.</typeparam>
/// <typeparam name="TResponse">The type of the response object.</typeparam>
public abstract class FunctionBase<TRequest, TResponse>
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly TimeSpan _timeout;

    /// <summary>
    ///     Initializes the base components of the function, including Dependency Injection, logging, and configuration.
    /// </summary>
    /// <param name="timeout">Optional timeout duration for the function execution. Default is 6 seconds.</param>
    protected FunctionBase(TimeSpan? timeout = null)
    {
        var configurationBuilder = new ConfigurationBuilder();
        ConfigureFunctionConfiguration(configurationBuilder);
        var configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        ConfigureFunctionServices(services, configuration);
        ConfigureFunctionLogging(services, configuration);
        _services = services.BuildServiceProvider();

        _logger = _services.GetService<ILoggerFactory>()?.CreateLogger(GetType().Name) ?? NullLogger.Instance;
        _timeout = timeout ?? TimeSpan.FromSeconds(6);
    }

    /// <summary>
    ///     Configures the function's configuration settings. By default, this adds environment variables.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add configuration sources to.</param>
    protected virtual void ConfigureFunctionConfiguration(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddEnvironmentVariables();
    }

    /// <summary>
    ///     Configures services for Dependency Injection. Allows derived classes to override service registration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration instance for the function.</param>
    protected virtual void ConfigureFunctionServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IConfiguration>(configuration);
    }

    /// <summary>
    ///     Configures logging for the function. Allows derived classes to override logging configuration.
    /// </summary>
    /// <param name="logging">The logging builder to configure logging providers.</param>
    /// <param name="logLevel">The minimum log level to be set for the logger.</param>
    /// <param name="configuration">The configuration instance for logging.</param>
    protected virtual void ConfigureFunctionLogging(ILoggingBuilder logging, LogLevel logLevel, IConfiguration configuration)
    {
        logging
            .SetMinimumLevel(logLevel)
            .ClearProviders()
            .AddProvider(new JsonLoggingProvider(logLevel, ConfigureLoggingJsonSerializerContext()));
    }

    /// <summary>
    ///     Provides a custom <see cref="JsonSerializerContext"/> for logging serialization.
    ///     Override this method to customize logging serialization.
    /// </summary>
    protected virtual JsonSerializerContext ConfigureLoggingJsonSerializerContext() => LoggingSerializationContext.Default;

    private void ConfigureFunctionLogging(IServiceCollection services, IConfiguration configuration)
    {
        var level = Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("GOA__LOG__LEVEL"), out var logLevel) ? logLevel : LogLevel.Information;
        services.AddLogging(logging => ConfigureFunctionLogging(logging, level, configuration));
    }

    /// <summary>
    ///     Handles the incoming lambda invocation.
    /// </summary>
    /// <param name="request">The incoming request to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var scope = _services.CreateScope();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await OnRequestAsync(request, cancellationToken);
            var response = await HandleRequestAsync(scope.ServiceProvider, request, cancellationToken);
            await OnResponseAsync(request, response, cancellationToken);

            return response;
        }
        catch (Exception exception)
        {
            _logger.LogFunctionError(exception);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogFunctionCompletion(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    ///     Handles the core logic of processing the request and generating the response.
    /// </summary>
    /// <param name="services">The service provider to resolve dependencies.</param>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    protected abstract Task<TResponse> HandleRequestAsync(IServiceProvider services, TRequest request, CancellationToken cancellationToken);

    /// <summary>
    ///     Allows for custom logic to be executed before handling the request.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    protected virtual Task OnRequestAsync(TRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    ///     Allows for custom logic to be executed after the request is handled and the response is generated.
    /// </summary>
    /// <param name="request">The original request.</param>
    /// <param name="response">The response generated from the request.</param>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    protected virtual Task OnResponseAsync(TRequest request, TResponse response, CancellationToken cancellationToken) => Task.CompletedTask;
}
