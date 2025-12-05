using Goa.Functions.Core.Bootstrapping;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core;

#pragma warning disable CS8618, CS1591

/// <summary>
/// Default implementation of <see cref="ILambdaBuilder"/> for configuring Lambda function hosting and lifecycle management
/// </summary>
public class LambdaBuilder : ILambdaBuilder
{
    private readonly IHostBuilder _builder;
    private JsonSerializerContext? _loggingSerializationContext;

    /// <inheritdoc />
    public IHostBuilder Host => _builder;

    /// <inheritdoc />
    public ILambdaRuntimeClient LambdaRuntime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaBuilder"/> class
    /// </summary>
    /// <param name="builder">The host builder to configure</param>
    /// <param name="lambdaRuntimeClient">Optional Lambda runtime client override</param>
    public LambdaBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
    {
        _builder = builder;

        var logLevel = LogLevel.Information;
        if (Enum.TryParse<LogLevel>("GOA__LOG__LEVEL", ignoreCase: true, out var goaLogLevel))
        {
            logLevel = goaLogLevel;
        }
        else if (Enum.TryParse<LogLevel>("LOGGING__LOGLEVEL__DEFAULT", ignoreCase: true, out var parsedLogLevel))
        {
            logLevel = parsedLogLevel;
        }

        LambdaRuntime = lambdaRuntimeClient ?? new LambdaRuntimeClient(logLevel);
    }

    /// <inheritdoc />
    public ILambdaBuilder WithConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        _builder.ConfigureAppConfiguration(configureDelegate);
        return this;
    }

    /// <inheritdoc />
    public ILambdaBuilder WithServices(Action<HostBuilderContext, IServiceCollection> servicesDelegate)
    {
        _builder.ConfigureServices(servicesDelegate);
        return this;
    }

    /// <inheritdoc />
    public ILambdaBuilder WithLoggingSerializationContext(JsonSerializerContext jsonSerializerContext)
    {
        _loggingSerializationContext = jsonSerializerContext;
        return this;
    }

    /// <inheritdoc />
    public async Task RunAsync(InitializationMode mode = InitializationMode.Parallel)
    {
        _builder.ConfigureServices((_, services) =>
        {
            services.AddLogging(b => b.AddGoaJsonLogging(_loggingSerializationContext ?? LoggingSerializationContext.Default));
        });

        var host = _builder.Build();

        // initialization tasks
        var tasks = host.Services.GetServices<ILambdaInitializationTask>().ToList();
        if (tasks.Count > 0)
        {
            if (mode == InitializationMode.Parallel)
            {
                await Task.WhenAll(tasks.Select(x => x.InitializeAsync()));
            }
            else
            {
                foreach (var task in tasks)
                    await task.InitializeAsync();
            }
        }

        await host.RunAsync();
    }
}
