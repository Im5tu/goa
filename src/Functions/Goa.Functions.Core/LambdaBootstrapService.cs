using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core;

/// <summary>
/// Hosted service that manages the Lambda bootstrap lifecycle for handling invocations
/// </summary>
/// <typeparam name="TRequest">The type of request payload</typeparam>
/// <typeparam name="TResponse">The type of response payload</typeparam>
public sealed class LambdaBootstrapService<TRequest, TResponse> : IHostedService
{
    private readonly ILogger<LambdaBootstrapService<TRequest, TResponse>> _logger;
    private readonly LambdaBootstrapper<TRequest, TResponse> _bootstrapper;
    private CancellationTokenSource? _cts;
    private Task? _task;

    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaBootstrapService{TRequest, TResponse}"/> class
    /// </summary>
    /// <param name="logger">Logger for the bootstrap service</param>
    /// <param name="context">JSON serialization context for request/response handling</param>
    /// <param name="onNext">Function to handle incoming Lambda invocations</param>
    /// <param name="lambdaRuntimeClient">Optional Lambda runtime client override</param>
    public LambdaBootstrapService(ILogger<LambdaBootstrapService<TRequest, TResponse>> logger, JsonSerializerContext context, Func<TRequest, InvocationRequest, CancellationToken, Task<TResponse>> onNext, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        _logger = logger;
        _bootstrapper = new LambdaBootstrapper<TRequest, TResponse>(context, lambdaRuntimeClient: lambdaRuntimeClient);
        _bootstrapper.OnNext(onNext);
    }

    /// <summary>
    /// Starts the Lambda bootstrap service and begins listening for invocations
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous start operation</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _task = _bootstrapper.RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the Lambda bootstrap service and cancels any ongoing operations
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the asynchronous stop operation</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null)
            return;

        try
        {
            await _cts.CancelAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Service stopped with exception");
        }
    }
}
