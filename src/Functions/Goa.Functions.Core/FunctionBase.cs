using System.Diagnostics;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Goa.Functions.Core;

public abstract class FunctionBase<TRequest, TResponse>
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly TimeSpan _timeout;

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

    // Allows overriding or extending configuration
    protected virtual void ConfigureFunctionConfiguration(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddEnvironmentVariables();
    }

    // Allows inheriting classes or tests to override default service registrations
    protected virtual void ConfigureFunctionServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IConfiguration>(configuration);
    }

    // Allows overriding or extending logging configuration
    protected virtual void ConfigureFunctionLogging(ILoggingBuilder logging, LogLevel logLevel, IConfiguration configuration)
    {
        logging
            .SetMinimumLevel(logLevel)
            .ClearProviders()
            .AddProvider(new JsonLoggingProvider(logLevel));
    }
    private void ConfigureFunctionLogging(IServiceCollection services, IConfiguration configuration)
    {
        // TODO :: document variable for logging
        var level = Enum.TryParse<LogLevel>(configuration["Log:Level"], out var logLevel) ? logLevel : LogLevel.Information;

        services.AddLogging(logging => ConfigureFunctionLogging(logging, level, configuration));
    }

    public async Task<TResponse> HandleAsync(TRequest request)
    {
        using var cancellationTokenSource = new CancellationTokenSource(_timeout);
        using var scope = _services.CreateScope();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await OnRequestAsync(request, cancellationTokenSource.Token);
            var response = await HandleRequestAsync(scope.ServiceProvider, request, cancellationTokenSource.Token);
            await OnResponseAsync(request, response, cancellationTokenSource.Token);

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

    protected abstract Task<TResponse> HandleRequestAsync(IServiceProvider services, TRequest request, CancellationToken cancellationToken);

    protected virtual Task OnRequestAsync(TRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnResponseAsync(TRequest request, TResponse response, CancellationToken cancellationToken) => Task.CompletedTask;
}
